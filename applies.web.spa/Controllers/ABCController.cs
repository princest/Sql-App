using EasyPump.Config;
using SqlFace.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace EasyPump.Controllers
{
    [AllowAnonymous]
    public class ABCController : Controller
    {
        // GET: ABC
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult uigrid()
        {
            return View();
        }
        //展现查询 .. 发送鉴权等信息,但是鉴权信息必须要重新校验
        public async Task<JsonResult> comm(string sqlkey, string param)
        {
            try
            {
                using (var svc = new OrmService(AppConfigs.sqlfaceconn))
                {
                    var setting = (await svc.FilterWhereAsync<SqlKeySetting>(s => s.SQLKEY == sqlkey && s.STS == "A")).FirstOrDefault();
                    if (null == setting)
                        throw new ApplicationException("没有配置[" + sqlkey + "]的查询语句,请见sqlkeysetting");
                    //userinfo = new API.DAL.MongoContext().Users.FindOne(Query.EQ("userName", this.User.Identity.Name));
                    setting.SQL = string.Format(setting.SQL, param);
                    var res = await fetchJson(setting);
                    if (!string.IsNullOrWhiteSpace(setting.PRIVATES) && setting.PRIVATES != "[]")
                    {
                        try
                        {
                            var priv = true;
                            if (!string.IsNullOrWhiteSpace(this.User.Identity.Name))
                            {

                                if (setting.EvalAuthorized(this.User.Identity.Name, Helper.Roles(this)))
                                    priv = false;
                            }
                            if (priv)
                            {
                                HashSet<string> privs = (HashSet<string>)Newtonsoft.Json.JsonConvert.DeserializeObject(setting.PRIVATES, typeof(HashSet<string>));
                                var names = ((List<string>)res.Data.GetType().GetProperty("names").GetValue(res.Data, null));
                                var data = (List<object[]>)res.Data.GetType().GetProperty("data").GetValue(res.Data, null);
                                for (int i = 0; i < names.Count; i++)
                                {
                                    if (privs.Contains(names[i]))
                                        data.ForEach(s => s[i] = "***");
                                }
                            }

                        }
                        catch { };
                    }

                    return res;
                }
            }
            catch (Exception e)
            {
                return Json(new { ErrorMsg = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [Authorize]
        public async Task<ActionResult> dmls(List<DmlIndex> dmls, SqlKeySetting sqlsetting, string memo)
        {
            using (var svc = new OrmService(AppConfigs.sqlfaceconn))
            {
                object msg;
                bool failed = false;
                var setting = (await svc.FilterWhereAsync<SqlKeySetting>(s => s.SQLKEY == sqlsetting.SQLKEY && s.STS == "A")).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(sqlsetting.DML_WHERE_COLS))
                    setting.DML_WHERE_COLS = sqlsetting.DML_WHERE_COLS;//主键列,前端重算:根据后端配置+视图列 综合决定                
                if (!setting.EvalAuthorized(this.User.Identity.Name, Helper.Roles(this)))
                    return Json("你没有权限修改执行!");
                var checklist = setting.ALLOWED_DML.Split(',');
                var headcheck = new List<string>();
                foreach (var ck in checklist)
                {
                    switch (ck.ToUpper())
                    {
                        case "U":
                            headcheck.Add("UPDATE " + setting.DML_ENTITY.ToUpper().Trim());
                            break;
                        case "I":
                            headcheck.Add("INSERT INTO " + setting.DML_ENTITY.ToUpper().Trim());
                            break;
                        case "D":
                            headcheck.Add("DELETE " + setting.DML_ENTITY.ToUpper().Trim());
                            break;
                        default:
                            break;
                    }
                }
                //行号,DML摘要,DML语句
                Dictionary<int, string[]> befores = new Dictionary<int, string[]>();
                //行号,执行结果:insert回填主键,update,和Delete则填结果数据即可
                Dictionary<string, string> outs = new Dictionary<string, string>();
                Hashtable safeSaveContext = new Hashtable();
                safeSaveContext.Add("setting", sqlsetting);
                safeSaveContext.Add("befores", befores);
                safeSaveContext.Add("outs", outs);
                //if(dmls.Any(s=> headcheck.Any(h=>s.IndexOf(h)>=0)))
                foreach (var dml in dmls)
                {
                    //如果在限定的头校验中都不匹配
                    if (headcheck.All(s => dml.sql.IndexOf(s) == -1))
                        throw new ApplicationException(dml.sql + ",不被允许!服务端校验未通过!");
                    befores.Add(dml.index, new string[] { dml.sql[0].ToString().ToUpper(), dml.sql });
                }
                try
                {
                    DMLHelper.safeRun(safeSaveContext);
                    msg = outs;
                }
                catch (Exception e)
                {
                    msg = (e.Message);
                    failed = true;
                }
                //var logservice = new OrmService<AP_ACTION_LOG_DBA>(db);
                //var seqservice = new OrmService<CustomSequence>(db);
                var type = msg.GetType();
                var msgmean = "";
                if (type == typeof(string))
                    msgmean = (string)msg;
                else
                    msgmean = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                var risk = String.Format("执行DML,sql-key={0},语句列表={1},数据列表={2}",
                          setting.SQLKEY, msgmean, memo);
                risk = risk.Substring(0, Math.Min(3900, risk.Length));

                var log = new AP_ACTION_LOG_DBA
                {
                    LOG_ID = CustomSequence.GetNextVal("AP_ACTION_LOG_DBA_ID", svc),
                    ACTION_BRIEF = setting.DML_ENTITY,
                    ACTION_IP = GetUserIp,
                    ACTION_PAGE = this.Request.RawUrl,
                    ACTION_PARAM = risk,
                    ACTION_RESULT = msgmean,
                    ACTION_TIME = DateTime.Now,
                    USER_ID = this.User.Identity.Name
                };
                await svc.CreateAsync<AP_ACTION_LOG_DBA>(log);
                var r = new { msg = msg, hasError = failed };
                return Json(r);
            }
        }

        [HttpGet]
        //打开连接
        public async Task<ActionResult> exlink(string tsqlkey, string[] param)
        {
            try
            {
                using (var svc = new OrmService(AppConfigs.sqlfaceconn))
                {
                    var setting = (await svc.FilterWhereAsync<SqlKeySetting>(s => s.SQLKEY == tsqlkey && s.STS == "A")).FirstOrDefault();
                    if (setting != null && setting.CLASS == "link")
                    {
                        object[] ppp = param.Cast<object>().ToArray();
                        var sql = string.Format(setting.SQL, ppp);
                        return Json(sql, JsonRequestBehavior.AllowGet);
                    }
                    throw new ApplicationException("不是合法的链接[" + tsqlkey + "]");
                }
            }
            catch (Exception e)
            {
                return Json(new { ErrorMsg = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        //执行存储
        public async Task<JsonResult> dojob(string tsqlkey, string param)
        {
            try
            {
                using (var svc = new OrmService(AppConfigs.sqlfaceconn))
                {
                    var setting = (await svc.FilterWhereAsync<SqlKeySetting>(s => s.SQLKEY == tsqlkey && s.STS == "A")).FirstOrDefault();
                    if (setting != null && setting.CLASS == "proc")
                    {
                        if (!setting.EvalAuthorized(this.User.Identity.Name, Helper.Roles(this)))
                            return Json("你没有权限执行!", JsonRequestBehavior.AllowGet);
                        var sqlresult = CalcMain.ExternalExecutor(setting.SQL, param, System.Configuration.ConfigurationManager.AppSettings[setting.DBCONN]);
                        return Json(sqlresult, JsonRequestBehavior.AllowGet);
                    }
                    throw new ApplicationException("没有配置执行语句[" + tsqlkey + "]!");
                }
            }
            catch (Exception e)
            {
                return Json(new { ErrorMsg = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [NonAction]
        private Task<JsonResult> fetchJson(SqlKeySetting sqlsetting, string delivery = null)
        {
            List<String> cols; List<Object[]> data; List<Type> colTypes;
            return Task.Run(() =>
            {
                try
                {
                    using (var ds = DBHelper.RunQuery(sqlsetting.SQL, ConfigurationManager.AppSettings[sqlsetting.DBCONN]).Tables[0])
                    {
                        DynmicService.GetTableRowsDataCompact(ds, out cols, out data, out colTypes);                        
                        return new JsonResult
                        {
                            Data = new
                            {
                                data = data,
                                ErrorMsg = "" + delivery,
                                names = cols,
                                types = colTypes.Select(s => s.Name.ToLower()),
                                sqlsetting = sqlsetting
                            },
                            ContentType = "application/json",
                            ContentEncoding = System.Text.Encoding.GetEncoding("gb2312"),
                            MaxJsonLength = Int32.MaxValue,
                            RecursionLimit = 64,
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                    }

                }
                catch (Exception e)
                {
                    cols = new List<string>();
                    data = new List<object[]>();
                    return new JsonResult()
                    {
                        Data = new
                        {
                            data = data,
                            ErrorMsg = e.Message,
                            names = cols,
                            sqlsetting = sqlsetting
                        },
                        ContentType = "application/json",
                        ContentEncoding = System.Text.Encoding.GetEncoding("gb2312"),
                        MaxJsonLength = Int32.MaxValue,
                        RecursionLimit = 64,
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
            });

        }
        [NonAction]
        private Task<JsonResult> fetchJson(string sql, string DbConn, string delivery = null)
        {
            return fetchJson(new SqlKeySetting { SQL = sql, DBCONN = DbConn }, delivery);
        }
        public async Task<JsonResult> showwonbrzdsts(string wo = "")
        {
            return await fetchJson("select wxhf_get_sts_from_whzd_careful('" + wo + "')V from dual", "dbHBIBMConnection");
        }
        //返回相关信息下的订单列表
        public async Task<JsonResult> filtercos(string s = "", bool m = false)
        {
            var sql = "";
            if (!m)
            {
                sql = "SELECT wxhf_co_choice('" + s + "') FROm dual";
                sql = DBHelper.getStringByRunQuery(sql, ConfigurationManager.AppSettings["dbHBIBMConnection"]);
            }
            else
            {
                sql = ConfigurationManager.AppSettings["bossparseHBwxh"];
                sql = Regex.Replace(sql, ":[0-9_a-zA-Z]+", s);
            }
            return await fetchJson(sql, "dbHBIBMConnection");

        }
        //返回订单下的sos
        public async Task<JsonResult> showco(string co = "")
        {
            var sql = "select * from " + ConfigurationManager.AppSettings["co_view"] + " where co_nbr ='" + co + "' ";
            var res = await fetchJson(sql, "dbHBIBMConnection");
            var data = (List<object[]>)res.Data.GetType().GetProperty("data").GetValue(res.Data, null);
            var ErrorMsg = (string)res.Data.GetType().GetProperty("ErrorMsg").GetValue(res.Data, null);
            if (data != null && data.Count == 0)
            {
                sql = string.Format(ConfigurationManager.AppSettings["co_view_repair"], co);
                var repair = await fetchJson(sql, "db_WDCRM_Connection", ErrorMsg);
                return repair;
            }
            else
                return res;
        }
        //返回申请单下的wos
        public async Task<JsonResult> showso(string so = "", string archlevel = "", string wo = "")
        {
            string sql;
            if (!string.IsNullOrWhiteSpace(wo))
                sql = "select * from " + string.Format(ConfigurationManager.AppSettings["so_view"], archlevel) + " where wo_nbr =" + wo;
            else if (!string.IsNullOrWhiteSpace(so))
                sql = "select * from " + string.Format(ConfigurationManager.AppSettings["so_view"], archlevel) + " where so_nbr ='" + so + "' order by wo_nbr";
            else
                return null;
            return await fetchJson(sql, "dbHBIBMConnection");
        }
        public async Task<JsonResult> showwohandle(string wo = "", string archlevel = "")
        {
            var sql = string.Format(ConfigurationManager.AppSettings["woh_view"], wo, archlevel);
            return await fetchJson(sql, "dbHBIBMConnection");
        }

        public ActionResult showWoTab(string wo, string archlevel = "")
        {
            return View();
        }
        public ActionResult showSoTab(string so, string archlevel = "")
        {
            return View();
        }
        public static string GetUserIp
        {
            get
            {
                string realRemoteIP = "";
                if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
                {
                    realRemoteIP = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].Split(',')[0];
                }
                if (string.IsNullOrEmpty(realRemoteIP))
                {
                    realRemoteIP = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
                if (string.IsNullOrEmpty(realRemoteIP))
                {
                    realRemoteIP = System.Web.HttpContext.Current.Request.UserHostAddress;
                }
                return realRemoteIP;
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
            base.Dispose(disposing);
        }
    }
    public class DmlIndex
    {
        public string sql { get; set; }
        public int index { get; set; }
    }
}