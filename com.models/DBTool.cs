using Oracle.DataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace SqlFace.Models
{
    public class DBHelper
    {
        private static readonly string defaultconnectionstring = System.Configuration.ConfigurationManager.AppSettings["dbConnection"];
        public static string ToString(Object obj)
        {
            string ret = string.Empty;
            if (obj == null)
                return string.Empty;
            try
            {
                ret = obj.ToString();
            }
            catch (Exception)
            {
                return ret;
            }

            return ret;
        }

        public static int ToInt(Object obj)
        {
            int ret = -1;
            if (obj == null)
                return -1;
            try
            {
                ret = Int32.Parse(ToString(obj));
            }
            catch (Exception)
            {
                return -1;
            }

            return ret;
        }

        public static void RunOracleProcedure(string cmdText, ref Dictionary<string, OracleParameter> param)
        {
            OracleConnection conn = new OracleConnection(defaultconnectionstring);
            conn.Open();
            try
            {
                OracleCommand cmd = new OracleCommand(cmdText, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (KeyValuePair<string, OracleParameter> op in param)
                {

                    cmd.Parameters.Add(op.Value);
                }
                cmd.ExecuteNonQuery();
                foreach (OracleParameter cmdparam in cmd.Parameters)
                {
                    param[cmdparam.ParameterName] = cmdparam;
                }
            }
            finally
            {
                conn.Close();

            }


        }

        public static void RunOracleProcedure(string cmdText, ref Dictionary<string, OracleParameter> param, string connection)
        {
            String ConnectionString = string.IsNullOrWhiteSpace(connection) ? defaultconnectionstring : connection;

            OracleConnection conn = new OracleConnection(ConnectionString);
            conn.Open();
            try
            {
                OracleCommand cmd = new OracleCommand(cmdText, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                foreach (KeyValuePair<string, OracleParameter> op in param)
                {
                    cmd.Parameters.Add(op.Value);
                }
                cmd.ExecuteNonQuery();
                foreach (OracleParameter cmdparam in cmd.Parameters)
                {
                    param[cmdparam.ParameterName] = cmdparam;
                }
                cmd.Dispose();
            }
            finally
            {
                conn.Close();
                conn.Dispose();

            }


        }

        private static void releasedblink(OracleConnection conn)
        {

            var sql = @"begin
                    rollback;
                    for links in (select db_link from v$dblink) loop
                    DBMS_SESSION.CLOSE_DATABASE_LINK (links.db_link);
                    end loop;
                    end;";
            try
            {
                using (OracleCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
            }

        }

        public static DataSet RunQuery(String QueryString, String connstr, bool freeMultiDbLink = false)
        {

            // Declare the connection string. This example uses Microsoft SQL Server 
            // and connects to the Northwind sample database.
            String ConnectionString = string.IsNullOrWhiteSpace(connstr) ? defaultconnectionstring : connstr;

            OracleConnection conn = new OracleConnection(ConnectionString);
            conn.Open();
            //releasedblink(conn);
            OracleDataAdapter DBAdapter;
            DataSet ResultsDataSet = new DataSet();

            try
            {

                // Run the query and create a DataSet.
                DBAdapter = new OracleDataAdapter(QueryString, conn);
                DBAdapter.Fill(ResultsDataSet);
                // Close the database connection.

            }
            catch (Exception ex)
            {
                throw ex;// new Exception("Unable to connect to the database.");
            }
            finally
            {
                if (freeMultiDbLink) releasedblink(conn);
                conn.Close();
            }

            return ResultsDataSet;

        }

        public static String getStringByRunQuery(String QueryString, string connstr = null)
        {

            // Declare the connection string. This example uses Microsoft SQL Server 
            // and connects to the Northwind sample database.
            String ConnectionString = string.IsNullOrWhiteSpace(connstr) ? defaultconnectionstring : connstr;

            OracleConnection conn = new OracleConnection(ConnectionString);
            conn.Open();

            OracleCommand oc = new OracleCommand(QueryString, conn);
            OracleDataReader reader = null;
            try
            {
                reader = oc.ExecuteReader();
            }
            catch
            {

                throw new ApplicationException("无效的查询语句" + QueryString);
            }

            string result = null;
            try
            {
                if (reader.Read())
                    result = ToString(reader[0]);
                reader.Close();
            }
            catch
            {
                result = string.Empty;
            }
            finally
            {
                conn.Close();
            }


            return result;

        }


        public static string addWhere(string orgSql, string condition)
        {
            string b = "\b";
            //string orgSql;
            //string condition = "1=1";
            List<string> lists = new List<string>();
            //lists.Add("select (select max(1) from dual) from dual where (select count(1) from dual)=1");
            //lists.Add("select * from dual order by 1");
            //lists.Add(" select * from dual");
            //lists.Add(" select * from(select * from dual where 1=2)");
            lists.Add(orgSql);
            string k = "failed";
            for (int i = 0; i < lists.Count; i++)
            {
                orgSql = lists[i];

                Regex r = new Regex("(?:\\(                            "
                                   + " (?>                           "
                                   + "    [^()]                      "
                                   + "   |                           "
                                   + "    \\( (?<DEPTH>)             "
                                   + "   |\\) (?<-DEPTH>)            "
                                   + " )*                            "
                                   + " (?(DEPTH)(?!))                "
                                   + "\\))?"
                                   + "[^()]*from\\s+[_a-zA-Z0-9.\"]+(?=(\\s+where(\\s+))?)"
                                  , RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                //Regex r = new Regex("(\\([^\\)]*\\))?\\s+from\\s+[_a-zA-Z0-9.\"]+(\\s+where\\s+)?", RegexOptions.IgnoreCase); // 定义一个Regex对象实例
                Match m = r.Match(orgSql); // 在字符串中匹配
                if (m.Success)
                {
                    string ret1 = m.Groups[0].Value;
                    //String ret = r.Replace(orgSql, "\\1 where " + condition + " and ");
                    string val;
                    val = m.Groups[1].ToString();
                    if (val.Equals(String.Empty) || null == val)
                    {
                        k = orgSql.Substring(0, m.Index) + m.Value + " where " + condition + " " + orgSql.Substring(m.Index + m.Length);
                    }
                    else
                    {
                        string wh = Regex.Replace(val, "where", "where " + condition + " and", RegexOptions.IgnoreCase);
                        k = orgSql.Substring(0, m.Index) + m.Value + wh + orgSql.Substring(m.Index + m.Length + val.Length);
                    }
                    return k;
                }

            }
            return k;
        }

        
        /// <summary>
        /// 请注意关闭reader
        /// </summary>
        /// <param name="QueryString"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        internal static IDataReader getReaderBySql(string QueryString, string connection=null)
        {
            // Declare the connection string. This example uses Microsoft SQL Server 
            // and connects to the Northwind sample database.
            String ConnectionString = string.IsNullOrWhiteSpace(connection) ? defaultconnectionstring : connection;

            OracleConnection conn = new OracleConnection(ConnectionString);
            conn.Open();

            OracleCommand oc = new OracleCommand(QueryString, conn);
            OracleDataReader reader = null;
            try
            {
                reader = oc.ExecuteReader();
            }
            catch
            {
                conn.Close();
                throw new ApplicationException("无效的查询语句" + QueryString);
            }
            return reader;

        }

        
        //internal static IList<KeyValuePair<string, string>> convertFormatStringToKVList(string fs, string pattern)
        //{
        //    Regex reg = new Regex(pattern);
        //    MatchCollection mc = reg.Matches(fs);
        //    string left = String.Copy(fs);
        //    int offset = 0;
        //    IList<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
        //    foreach (Match m in mc)
        //    {
        //        left = left.Remove(m.Index - offset, m.Length);
        //        offset += m.Length;
        //        string entry = m.Groups["entry"].Value;
        //        string key = m.Groups["key"].Value;
        //        string value = m.Groups["value"].Value;
        //        key = String.IsNullOrWhiteSpace(key) ? "" : key.Trim();
        //        value = String.IsNullOrWhiteSpace(key) ? "" : value.Trim();
        //        KeyValuePair<string, string> kv = new KeyValuePair<string, string>(key, value);
        //        list.Add(kv);
        //    }
        //    return list;
        //}
        internal static DataView KVCommaDataView(IList<KeyValuePair<string, string>> KVS)
        {
            // Create new DataTable and DataSource objects.
            DataTable table = new DataTable();

            // Declare DataColumn and DataRow variables.
            DataColumn column;
            DataRow row;
            DataView view;

            // Create new DataColumn, set DataType, ColumnName and add to DataTable.    
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "key";
            table.Columns.Add(column);

            // Create second column.
            column = new DataColumn();
            column.DataType = Type.GetType("System.String");
            column.ColumnName = "value";
            table.Columns.Add(column);

            // Create new DataRow objects and add to DataTable.     
            foreach (var kv in KVS)
            {
                row = table.NewRow();
                row["key"] = kv.Key;
                row["value"] = kv.Value;
                table.Rows.Add(row);
            }

            // Create a DataView using the DataTable.
            view = new DataView(table);

            // Set a DataGrid control's DataSource to the DataView.
            //dataGrid1.DataSource = view;
            return view;
        }

    }
    internal enum DMLType
    {
        U,
        D,
        I
    }
    internal enum DynamicInmute
    {
        returnclause,
        anonymousblock,
        procedure,
        function
    }
    public class DMLHelper
    {


        public static void safeRun(System.Collections.Hashtable safeSaveContext)
        {
            Dictionary<int, string[]> befores = (Dictionary<int, string[]>)safeSaveContext["befores"];
            Dictionary<string, string> outs = (Dictionary<string, string>)safeSaveContext["outs"];
            SqlKeySetting setting = (SqlKeySetting)safeSaveContext["setting"];
            DMLHelper.SaftRunOracleStatement(befores.Values.Select(s => s[1]).ToArray(), setting.DBCONN, outs, befores, setting.DML_WHERE_COLS);
        }

        /// <summary>
        /// 安全(数据对比)执行SQL-dml语句
        /// </summary>
        /// <param name="dmls">dml列表</param>
        /// <param name="conn">db连接</param>
        /// <param name="outs">输出:序, 结果</param>
        /// <param name="befores">输入:序, SQL</param>
        /// <param name="pklike">主键</param>
        internal static void SaftRunOracleStatement(string[] dmls, string conn, Dictionary<string, string> outs, Dictionary<int, string[]> befores, string pklike)
        {
            int maxListSize = 0;
            if (!int.TryParse(System.Configuration.ConfigurationManager.AppSettings["MAX-DML-COUNT"], out maxListSize))
                maxListSize = 100;
            if (dmls.Length > maxListSize)
                throw new ApplicationException("不允许一次操作太多的数据,请使用专业的数据库工具!");
            RunOracleStatement(dmls, conn, outs, befores, pklike);
        }
        internal static void RunOracleStatement(string[] dmls, string connection, Dictionary<string, string> outs,
          Dictionary<int, string[]> safecheck = null, string pklike = "ROWID")
        {
            using (var con = new OracleConnection(connection))
            {

                try
                {
                    con.Open();
                    using (var trans = con.BeginTransaction())
                    {
                        try
                        {
                            if (safecheck == null)
                                for (int i = 0; i < dmls.Length; i++)
                                {
                                    var dml = dmls[i];
                                    OracleCommand cmd = new OracleCommand(dml, con);
                                    var rt = cmd.ExecuteNonQuery();
                                    outs.Add(i.ToString(), DBHelper.ToString(rt));
                                }
                            else
                            {
                                //saftloop(msg, safecheck, con, trans);
                                foreach (var item in safecheck)
                                {
                                    DMLType dmltype;
                                    try
                                    {
                                        dmltype = (DMLType)Enum.Parse(typeof(DMLType), item.Value[0], true);
                                    }
                                    catch
                                    {
                                        throw new ApplicationException("无法识别的DML,只能接受insert/update/delete");
                                    }

                                    using (OracleCommand cmd = con.CreateCommand())
                                    {
                                        cmd.CommandText = item.Value[1];
                                        int rt = -1; string ret = null;
                                        switch (dmltype)
                                        {
                                            case DMLType.U:
                                                rt = cmd.ExecuteNonQuery();
                                                if (rt == 0)
                                                {
                                                    outs.Add(item.Key.ToString(), "该行数据发生变化,或者被删了,请刷新");
                                                    throw new ApplicationException(
                                                         "第" + (item.Key + 1) + "行数据发生变化,或者被删了,请刷新!");
                                                }
                                                else
                                                    outs.Add(item.Key.ToString(), "更改数:" + rt);
                                                break;
                                            case DMLType.D:
                                                rt = cmd.ExecuteNonQuery();
                                                outs.Add(item.Key.ToString(), "删除数:" + rt);
                                                break;
                                            case DMLType.I:
                                                cmd.CommandText = cmd.CommandText;
                                                rt = cmd.ExecuteNonQuery(); // an INSERT is always a Non Query
                                                outs.Add(item.Key.ToString(), (null == ret ? "插入数:" + rt : ret));
                                                //无法捕获ORACLE return value,除非建存储过程来捕获..侵入数据库式
                                                //try
                                                //{
                                                //    throw new NotImplementedException();
                                                //    DynamicInmute t =(DynamicInmute)Enum.Parse(typeof(DynamicInmute), ConfigurationManager.AppSettings["DynamicInmute"],true);
                                                //    tryDynInmute(pklike, cmd, ref rt, ref ret, DynamicInmute.anonymousblock);
                                                //}
                                                //catch
                                                //{

                                                //}
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }

                }
                catch
                {
                    throw;
                }
                finally
                {
                    con.Close();
                };
            }


        }


        private static void tryDynInmute(string pklike, OracleCommand cmd, ref int rt, ref string ret, DynamicInmute ditype)
        {
            try
            {
                switch (ditype)
                {
                    case DynamicInmute.returnclause:
                        cmd.CommandText = cmd.CommandText + "return " + pklike + " into :univalue";
                        cmd.Parameters.Add(new OracleParameter("univalue", OracleDbType.Varchar2, ParameterDirection.ReturnValue));
                        rt = cmd.ExecuteNonQuery(); // an INSERT is always a Non Query
                        ret = Convert.ToString(cmd.Parameters["univalue"].Value);
                        break;
                    case DynamicInmute.anonymousblock:
                        //https://community.oracle.com/thread/475515
                        string anonymous_block = "begin " +
                               cmd.CommandText + "return " + pklike + " into :v;" +
                               "end;";
                        //OracleParameter p_1 = new OracleParameter("1", OracleDbType.Decimal, 50, ParameterDirection.Input);
                        OracleParameter p_2 = new OracleParameter("v", OracleDbType.Varchar2, ParameterDirection.Output);
                        //OracleParameter p_3 = new OracleParameter("3", OracleDbType.Varchar2, "HERE", ParameterDirection.Input);
                        //OracleParameter p_4 = new OracleParameter("4", OracleDbType.RefCursor, ParameterDirection.Output);
                        //OracleParameter p_5 = new OracleParameter("5", OracleDbType.Varchar2, ParameterDirection.Output);
                        //OracleParameter p_6 = new OracleParameter("6", OracleDbType.Decimal, ParameterDirection.Output);
                        //cmd.Parameters.Add(p_1);
                        cmd.Parameters.Add(p_2);
                        // cmd.Parameters.Add(p_5);
                        //cmd.Parameters.Add(p_6);
                        rt = cmd.ExecuteNonQuery();
                        ret = DBHelper.ToString(p_2.Value);

                        break;
                    case DynamicInmute.procedure:
                    case DynamicInmute.function:
                        throw new NotImplementedException(Enum.GetName(typeof(DynamicInmute), ditype) + "这种方式还没实现!");
                    default:
                        break;
                }
            }

            catch (Exception e)
            {
                throw e;
                //if (e.Message.IndexOf("ORA-22816") != -1)
                //    throw;
                //cmd.CommandText = cmd.CommandText;
                //rt = cmd.ExecuteNonQuery(); // an INSERT is always a Non Query
            }
        }

    }

}