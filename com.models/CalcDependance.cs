using Oracle.DataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Collections.Specialized;

namespace SqlFace.Models
{
    public class TableMeta
    {
        public List<string> cols { get; set; }
        public List<object[]> data { get; set; }
        public bool isBackEndPaged { get; set; }
        public int currentPage { get; set; }
        public int numPerPage { get; set; }
        public int dataTotallSize { get; set; }
        public string pageby { get; set; }
    }
    public class ExecutionIO
    {
        //    internal string SqlBlock { get; set; }
        //    internal string Descript { get; set; }
        //    internal Dictionary<string, object> ParamsContext { get; set; }
        public bool hasError { get; set; }
        public string msg { get; set; }
        public bool HasTable { get; set; }
        public TableMeta TableMeta { get; set; }
    }
    public class RenderContext
    {
        public EasyHandler handler { get; set; }
        public IList<EasyHandlerParam> parameters { get; set; }


        public HandlerExtralConfig frontConfig { get; set; }

        public IList<ParamGraphMap> DAG { get; set; }


        public Dictionary<decimal, string> dict { get; set; }

        public Dictionary<string, List<Options>> OptionSelects { get; set; }

        public Dictionary<decimal, HashSet<decimal>> depends { get; set; }

        //标量值:可能是 选项型,也可能是输入型
        public Dictionary<string, object> paramsScaledValues { get; set; }
        public HashSet<decimal> Triggers { get; set; }
        public ExecutionIO ExecutionIO { get; set; }
    }

    public class Options
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }


    enum DDLTipsShowWay
    {
        Server,
        Client
    }

    static class _helper
    {
        public static string downReal(this string keyword)
        {
            try
            {
                return System.Configuration.ConfigurationManager.ConnectionStrings[keyword].ConnectionString;
            }
            catch
            {
                return System.Configuration.ConfigurationManager.AppSettings[keyword] ?? keyword;
            }
        }

    }

    public class CalcMain
    {
        private RenderContext context;
        private MatchEvaluator me;
        private Dictionary<string, string> environment;
        private string environRegex;
        [Obsolete("已启用",true)]
        public CalcMain(RenderContext context)
        {
            this.context = context;
        }
        public CalcMain(RenderContext context, Dictionary<string, string> environment =null , string environRegex = @":\[([_0-9a-zA-Z]+)\]", MatchEvaluator me = null)
        {
            this.context = context;
            this.environment = environment;
            this.environRegex = environRegex;
            if (me != null)
                this.me = me;
            else
                this.me = (delegate (Match m)
                {
                    if (environment != null && environment.ContainsKey(m.Groups[1].Value))
                        return environment[m.Groups[1].Value];
                    else
                        return m.Value;
                });
        }
        private string ckout(string old)
        {
            Regex r = new Regex(environRegex);
            if (!r.IsMatch(old)) return old;
            return r.Replace(old, me);
        }
        public string ExeSqlBlock()
        {
            //var exeparam = ServiceStack.Text.JsonSerializer.SerializeToString(context.paramsScaledValues);
            var exeparam = Newtonsoft.Json.JsonConvert.SerializeObject(context.paramsScaledValues);

            string cmdText = "WXHP_do_sql_Block";
            Dictionary<string, OracleParameter> cmdp = new Dictionary<string, OracleParameter>();
            cmdp.Add("sql_text", new OracleParameter("sql_text", OracleDbType.Varchar2, 4000, this.ckout(context.handler.SQL_BLOCK), System.Data.ParameterDirection.Input));
            cmdp.Add("ip_bind_vars", new OracleParameter("ip_bind_vars", OracleDbType.Varchar2, 4000, exeparam, System.Data.ParameterDirection.Input));
            cmdp.Add("returnstr", new OracleParameter("returnstr", OracleDbType.Varchar2, 4000, null, System.Data.ParameterDirection.Output));

            DBHelper.RunOracleProcedure(cmdText, ref cmdp, context.handler.DB_CONNECTION.downReal());
            var returnstr = DBHelper.ToString(cmdp["returnstr"].Value);
            if (returnstr.ToUpper().Equals("NULL"))
            {
                returnstr = string.Empty;
            }
            return returnstr;
        }
        public static string ExternalExecutor(string sqlblock, object pstr, string connconfig)
        {
            var exeparam = "";
            if (pstr != null)
                if (pstr.GetType() == typeof(string))
                    exeparam = (string)pstr;
                else
                    exeparam = ServiceStack.Text.JsonSerializer.SerializeToString(pstr);

            string cmdText = "WXHP_do_sql_Block";
            Dictionary<string, OracleParameter> cmdp = new Dictionary<string, OracleParameter>();
            cmdp.Add("sql_text", new OracleParameter("sql_text", OracleDbType.Varchar2, 4000, sqlblock, System.Data.ParameterDirection.Input));
            cmdp.Add("ip_bind_vars", new OracleParameter("ip_bind_vars", OracleDbType.Varchar2, 4000, exeparam, System.Data.ParameterDirection.Input));
            cmdp.Add("returnstr", new OracleParameter("returnstr", OracleDbType.Varchar2, 4000, null, System.Data.ParameterDirection.Output));

            DBHelper.RunOracleProcedure(cmdText, ref cmdp, connconfig);
            var returnstr = DBHelper.ToString(cmdp["returnstr"].Value);
            if (returnstr.ToUpper().Equals("NULL"))
            {
                returnstr = string.Empty;
            }
            return returnstr;
        }
        public string GetQuery()
        {
            context.ExecutionIO.HasTable = true;
            List<object[]> data; List<string> cols; List<Type> colTypes;
            int pageindex = this.context.ExecutionIO.TableMeta.currentPage;
            int pagesize = this.context.ExecutionIO.TableMeta.numPerPage;
            string pageby = "";
            string paging = CalcDependance.exefullSql(this.ckout(context.handler.getParaedSql()), context.paramsScaledValues);
            string dbconnectionspec = context.handler.DB_CONNECTION.downReal();
            try
            {
                //后端分页
                if (context.ExecutionIO.TableMeta.isBackEndPaged)
                {
                    if (this.context.ExecutionIO.TableMeta.dataTotallSize < 0)
                    {
                        this.context.ExecutionIO.TableMeta.dataTotallSize = DBHelper.ToInt(DBHelper.getStringByRunQuery(
                            string.Format("select count(1) from ({0} )aname", paging)
                            , dbconnectionspec));
                        //datasize = int.Parse( .getValueBySql(string.Format("select count(1) from ({0} )aname", qry), conn));
                    }

                    int pstart, pend;
                    pstart = (pageindex - 1) * pagesize + 1; // zero based in sqlserver top,but 1 based for Oracle
                    if (this.context.ExecutionIO.TableMeta.dataTotallSize > pstart)
                        pend = Math.Min(this.context.ExecutionIO.TableMeta.dataTotallSize, (pageindex) * pagesize); //zero based
                    else
                        pend = (pageindex) * pagesize + 1;
                    if (pageby != "sql2000andbelow")
                    {
                        paging = string.Format(" select * from (select rownum as \"#\", t.* from ({2}) t) where \"#\" between {0} and {1}  ",
                            pstart, pend, paging);
                    }
                }

                using (var ds = DBHelper.RunQuery(paging, dbconnectionspec).Tables[0])
                {
                    DynmicService.GetTableRowsDataCompact(ds, out cols, out data, out colTypes);
                    context.ExecutionIO.TableMeta.data = data;
                    context.ExecutionIO.TableMeta.cols = cols;
                    if (!context.ExecutionIO.TableMeta.isBackEndPaged)
                    {
                        this.context.ExecutionIO.TableMeta.dataTotallSize = data.Count;
                    }
                    return "";
                }

            }
            catch (Exception e)
            {
                context.ExecutionIO.TableMeta.cols = new List<string>();
                context.ExecutionIO.TableMeta.data = new List<object[]>();
                context.ExecutionIO.hasError = true;
                return e.Message;
            }
        }



    }

    public class CalcDependance
    {
        protected enum mark
        {
            /// 临时状态
            temporary = 1,
            /// 访问过
            permanent = 2,
            /// 为访问过
            unmarked = 0
        }

        private RenderContext context;

        private Dictionary<decimal, mark> allnodes;
        private Dictionary<decimal, bool> nodescaled;

        private Dictionary<decimal, EasyHandlerParam> details;

        private string select = System.Configuration.ConfigurationManager.AppSettings["OPTIONS_SELECT"];
        private string DDLtipswaySetting = System.Configuration.ConfigurationManager.AppSettings["OPTIONS_TIPS_FROM"];


        private DDLTipsShowWay way;
        private string[] tipsFilter = new string[0];

        public CalcDependance(RenderContext context)
        {
            this.context = context;
            this.allnodes = new Dictionary<decimal, mark>();
            this.nodescaled = new Dictionary<decimal, bool>();
            //this.triggers = new HashSet<decimal> { trigger };
            details = new Dictionary<decimal, EasyHandlerParam>();
            foreach (var item in context.parameters)
            {
                allnodes.Add(item.HANDLER_PARAM_ID, mark.unmarked);
                nodescaled.Add(item.HANDLER_PARAM_ID, context.Triggers.Contains(item.HANDLER_PARAM_ID));
                details.Add(item.HANDLER_PARAM_ID, item);
            }
            if ("Server".Equals(DDLtipswaySetting))
                way = DDLTipsShowWay.Server;
            else
                way = DDLTipsShowWay.Client;

            if (!string.IsNullOrWhiteSpace(select))
                tipsFilter = select.Split(',');
        }
        public void EvolveItOut()
        {
            foreach (var item in context.parameters.Select(s => s.HANDLER_PARAM_ID))
            {

                if (!visit(item))
                {
                    throw new ApplicationException("非DAG");
                }

            }
        }

        private bool visit(decimal node)
        {
            mark marking = allnodes[node];

            if (marking == mark.temporary)//not a Directed Acylic Graph
            {
                return false;
            }
            if (marking == mark.unmarked)
            {
                var pa = details[node];
                allnodes[node] = mark.temporary;
                HashSet<decimal> dependsnodes;
                if (!context.depends.ContainsKey(node))
                    dependsnodes = new HashSet<decimal>();
                else
                    dependsnodes = context.depends[node];
                foreach (var m in dependsnodes) //遍历
                {
                    if (!visit(m))
                    {
                        return false;
                    }
                    if (context.Triggers.Contains(m))// 如果依赖的参数变化,那么当前参数值会发生变化,不再起效
                    {
                        context.Triggers.Add(node);
                        nodescaled[node] = false;
                    }
                }
                //依赖访问完毕
                allnodes[node] = mark.permanent;
                if (!context.Triggers.Contains(node)) //如果不受其他参数影响
                    nodescaled[node] = context.paramsScaledValues.ContainsKey(pa.PARAM_NAME) && context.paramsScaledValues[pa.PARAM_NAME] != null;
                //计算节点值,null,[],val
                if (dependsnodes.Count == 0 || dependsnodes.All(s => nodescaled[s]))
                {
                    if (!nodescaled[node])//没有获得标值
                    {
                        var unscaledVal = ParamEvalToResultScaled(details[node]);
                        //var clean = scalingEval(node, unscaledVal);
                        nodescaled[node] = unscaledVal;//可能为标量,也可能不是,但是 paramsScaledValues/selectables 得到了值
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// 计算参数值,并返回是否为标量.如果是选项型,则生成选项->selectables;标量型则放入->参-值表:paramsScaledValues
        /// </summary>
        /// <param name="pa"></param>
        /// <returns></returns>

        private bool ParamEvalToResultScaled(EasyHandlerParam pa)
        {
            var scaled = false;
            if (pa.CONTROL_DATASOURCE_TYPE == "sql")
            {
                object r = null;
                var sql = exefullSql(pa.CONTROL_DATASOURCE, context.paramsScaledValues);
                var uncalcable = (context.depends.ContainsKey(pa.HANDLER_PARAM_ID) && context.depends[pa.HANDLER_PARAM_ID].Any(s => !nodescaled[s]));
                if (pa.ResultIsList())
                {
                    List<Options> options = new List<Options>();
                    if (!uncalcable) //有计算结果
                    {
                        var queresult = DBHelper.RunQuery(sql, pa.DB_CONNECTION.downReal());
                        options = generateOption(pa.CONTROL_DATASOURCE_KEY, pa.CONTROL_DATASOURCE_VALUE, queresult);
                        if (null != options && options.Count > 0)
                        {
                            var optionsHasTips = tipsFilter.Any(tip => options[0].Name.IndexOf(tip) >= 0);
                            if (optionsHasTips && way == DDLTipsShowWay.Client)
                                options.RemoveAt(0);
                            if (options.Count == 1)//标量
                            {
                                r = options[0].Value;
                                scaled = true;
                            }
                        }

                    }
                    updateOptions(pa.PARAM_NAME, options);
                    updateValues(pa.PARAM_NAME, r);
                    return scaled;
                }
                else
                {
                    if (!uncalcable)
                    {
                        //r = DBHelper.getStringByRunQuery(sql, pa.DB_CONNECTION);
                        r = DynmicService.RunScalar(sql, pa.DB_CONNECTION.downReal());
                        scaled = true;
                    }
                    updateValues(pa.PARAM_NAME, r);
                    return scaled;
                }

            }
            else
                if (pa.CONTROL_DATASOURCE_TYPE == "formatbycomma")
            {
                object val = null;
                List<Options> options = ParseToOptions(pa.CONTROL_DATASOURCE);
                if (options.Count == 1)//标量
                {
                    val = options[0].Value;
                    scaled = true;
                }
                updateOptions(pa.PARAM_NAME, options);
                updateValues(pa.PARAM_NAME, val);
                return scaled;
            }
            else
            {
                updateValues(pa.PARAM_NAME, null);//默认的取值方法,目前只有sql ,comma
                return false;//
            }
        }

        private void updateOptions(string p, List<Options> options)
        {
            if (context.OptionSelects.ContainsKey(p))
                context.OptionSelects[p] = options;
            else
                context.OptionSelects.Add(p, options);
        }
        private void updateValues(string p, object value)
        {
            if (context.paramsScaledValues.ContainsKey(p))
                context.paramsScaledValues[p] = value;
            else
                context.paramsScaledValues.Add(p, value);
            //nodescaled[id] = scaled && value != null;
        }
        /// <summary>
        /// 返回sql语句
        /// </summary>
        /// <param name="ParamedSql"></param>
        /// <returns></returns>
        public static string exefullSql(string ParamedSql, Dictionary<string, object> paramsScaledValues)
        {
            string pattern;
            string smartPattern;
            Regex reg = null;
            Regex smartreg = null;
            string utilmateSql = ParamedSql;

            if (null != paramsScaledValues)
                foreach (KeyValuePair<string, object> kv in paramsScaledValues)
                {
                    pattern = ":" + kv.Key + "\\b";
                    smartPattern = @"(?<=in\s*(\(\s*)?)" + pattern;
                    reg = new Regex(pattern, RegexOptions.IgnoreCase);
                    smartreg = new Regex(smartPattern, RegexOptions.IgnoreCase);
                    bool test = smartreg.IsMatch(ParamedSql);
                    if (test)
                    {
                        string in_formatted = "(null)";
                        if (null != kv.Value)
                            //用户多选
                            if (typeof(IEnumerable<object>).IsAssignableFrom(kv.Value.GetType()))
                            {
                                var fill = "null";
                                in_formatted = "({0})";
                                var vs = (IEnumerable<object>)kv.Value;
                                if (vs.Count() > 0)
                                {
                                    var ix = "";
                                    foreach (var vl in vs)
                                    {
                                        ix += string.Format("'{0}',", DBHelper.ToString(vl));
                                    }
                                    fill = ix.TrimEnd(',');
                                }
                                in_formatted = string.Format(in_formatted, fill);
                            }
                            //else if (kv.Value.IndexOf(',') >= 0)
                            //    in_formatted = "('" + kv.Value.Replace(",", "','") + "')";
                            else
                                in_formatted = "('" + kv.Value + "')";
                        utilmateSql = smartreg.Replace(utilmateSql, in_formatted);
                    }
                    test = reg.IsMatch(ParamedSql);
                    if (test)
                    {
                        utilmateSql = reg.Replace(utilmateSql, "q'#" + DBHelper.ToString(kv.Value) + "#'");
                    }

                }
            return utilmateSql;

        }

        /// <summary>
        /// 根据SQL语句生成选项
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        private List<Options> generateOption(string name, string value, DataSet dataSet)
        {
            var result = new List<Options>();
            var cols = dataSet.Tables[0].Columns;
            foreach (DataRow item in dataSet.Tables[0].Rows)
            {

                if (cols.Count == 1) //一个参数
                {
                    var v = DBHelper.ToString(item[0]);
                    result.Add(new Options { Name = v, Value = v });
                }
                else
                {
                    string v = "", v2 = "";
                    try
                    {
                        v = DBHelper.ToString(item[name]);
                        v2 = DBHelper.ToString(item[value]);
                    }
                    catch (Exception)
                    {
                        v = DBHelper.ToString(item[0]);
                        v2 = DBHelper.ToString(item[1]);
                    }
                    result.Add(new Options { Name = v, Value = v2 });
                }
            }
            return result;
        }
        /// <summary>
        /// 根据键值格式生成选项
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private List<Options> ParseToOptions(string fs, string pattern = "((?<entry>(?<key>[^=]+)=(?<value>[^,]*),?))")
        {


            Regex reg = new Regex(pattern);
            MatchCollection mc = reg.Matches(fs);
            string left = String.Copy(fs);
            int offset = 0;
            List<Options> list = new List<Options>();
            foreach (Match m in mc)
            {
                left = left.Remove(m.Index - offset, m.Length);
                offset += m.Length;
                string entry = m.Groups["entry"].Value;
                string key = m.Groups["key"].Value;
                string value = m.Groups["value"].Value;
                key = String.IsNullOrWhiteSpace(key) ? "" : key.Trim();
                value = String.IsNullOrWhiteSpace(key) ? "" : value.Trim();

                var optionsHasTips = tipsFilter.Any(tip => key.IndexOf(tip) >= 0);
                if (optionsHasTips && way == DDLTipsShowWay.Client)
                    continue;

                list.Add(new Options { Name = key, Value = value });
            }
            return list;
        }



    }
}
