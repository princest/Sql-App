using EasyPump.Common;
using EasyPump.Config;
using SqlFace.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using WHODS.BLL;

namespace EasyPump.Controllers
{
    [RoutePrefix("api/XAngular")]
    public class XAngularController : IpApiController
    {
        // GET api/xangular/5
        [Route("{id:int}")]
        public object Get(decimal id)
        {
            return GetRenders(id, null).Result;
        }
        [NonAction]
        public static Task<RenderContext> GetRenders(decimal id, RenderContext context)
        {

            using (var svc = new OrmService(AppConfigs.sqlfaceconn))
            {

                var handler = svc.GetById<EasyHandler>(id);
                if (null == handler || handler.STS != "A")
                    throw new ApplicationException("不存在SQL处理器");


                var t_ps = svc.FilterWhereAsync<EasyHandlerParam>(s => s.HANDLER_ID == id);

                var t_ex_config = svc.GetByIdAsync<HandlerExtralConfig>(id);

                var t_dagsides = svc.FilterWhereAsync<ParamGraphMap>(c => c.GRAPH_ID == id);

                Task.WaitAll(new Task[] { t_ps, t_ex_config, t_dagsides });

                var _t_ps = t_ps.ContinueWith(task =>
                {
                    Dictionary<decimal, string> dict = new Dictionary<decimal, string>();
                    foreach (var item in task.Result)
                    {
                        dict.Add(item.HANDLER_PARAM_ID, item.PARAM_NAME);
                    }
                    return dict;
                });
                var _t_dagsides = t_dagsides.ContinueWith(task =>
                {
                     //原始的键依赖关系
                     Dictionary<decimal, HashSet<decimal>> depends = new Dictionary<decimal, HashSet<decimal>>();
                    foreach (var item in task.Result)
                    {
                        if (!depends.ContainsKey(item.FROMKEY))
                            depends.Add(item.FROMKEY, new HashSet<decimal>());
                        depends[item.FROMKEY].Add(item.TOKEY);
                    }
                    return depends;
                });

                ////////逆向依赖关系 这个也可以在客户端或者服务端计算            
                //Dictionary<string, HashSet<string>> bydepend = new Dictionary<string, HashSet<string>>();

                RenderContext vm = null;
                if (null != context) //选择性的从客户端取值,防止被黑,安全模式!
                {
                    vm = context;
                    vm.handler = handler;
                    vm.parameters = t_ps.Result;
                }
                else
                {
                    vm = new RenderContext
                    {
                        handler = handler,
                        parameters = t_ps.Result.OrderBy(s=>s.PARAM_NAME_C).OrderBy(s => s.ORDER_ID).ToList(),
                        frontConfig = t_ex_config.Result,
                        DAG = t_dagsides.Result,
                        dict = _t_ps.Result,
                        paramsScaledValues = new Dictionary<string, object>(),
                        depends = _t_dagsides.Result,
                        Triggers = new HashSet<decimal>(),
                        OptionSelects = new Dictionary<string, List<Options>>()
                    };
                }

                EvolvingOut(vm);
                return Task.FromResult(vm);
            }
        }

        private static void EvolvingOut(RenderContext vm)
        {
            CalcDependance cal = new CalcDependance(vm);
            //键值映射表 放入CalcDependance
            cal.EvolveItOut();
        }

        // routeTemplate: "api/{controller}/{ExCodeGet}/{param}"

        [Route("ExCodeGet/{ec}")]
        [HttpGet]
        public async Task<object> ExCodeGet(string ec)
        {
            using (var svc = new OrmService(AppConfigs.sqlfaceconn))
            {
                if (string.IsNullOrWhiteSpace(ec))
                    return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
                var ee = (await svc.FilterWhereAsync<ExternalEntryMap>(s => s.Entry_Code == ec)).FirstOrDefault(); //db.ExternalEntryMaps.Where(s => s.EntryCode == EC).Single();            
                if (ee == null)
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                var id = ee.Engine_ID;
                return Get(id);
            }
        }

        [Route("ExecuteSubSql/{muteid}")]
        [HttpPost]
        //[Authorize]
        public async Task<object> ExecuteSubSql(decimal muteid, [FromBody] RenderContext context)
        {
            bool EvolvesSafe = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["EvolvesSafe"]);//是否全盘接受客户端参数

            var valuecontext = context.paramsScaledValues;
            if (EvolvesSafe)//安全方式,从服务端加载参数.
            {
                var mainid = context.handler.HANDLER_ID;
                return await GetRenders(mainid, context);
            }
            else
            {
                //迭代计算
                EvolvingOut(context);
                return context;
            }
        }
        [Route("ExecuteHandler/{id}")]
        [HttpPost]

        public async Task<object> ExecuteHandler(decimal id, [FromBody] RenderContext context)
        {
            return await _ExecuteHandler(id, context);
        }

        [NonAction]
        //[Authorize]
        private Task<RenderContext> _ExecuteHandler(decimal id, RenderContext context)
        {
            bool EvolvesSafe = true;//Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["EvolvesSafe"]);//是否全盘接受客户端参数
            bool isGetPagedDataing = null != context.ExecutionIO && context.ExecutionIO.HasTable;
            var valuecontext = context.paramsScaledValues;
            if (EvolvesSafe)//安全方式,从服务端加载参数.
            {
                var mainid = id;
                using (var svc = new OrmService(AppConfigs.sqlfaceconn))
                {
                    //List<String> cols = new List<string>();
                    //List<Object[]> data = new List<object[]>();
                    ExecutionIO outputMsg = context.ExecutionIO;

                    var handler = svc.GetByIdAsync<EasyHandler>(id).Result;
                    bool isSelect = "SELECT".Equals(handler.SQL_CMD_TYPE);
                    context.handler = handler;//防止被黑

                    //var logservice = new OrmService<AP_ACTION_LOG_DBA>(svc);
                    //var seqservice = new OrmService<CustomSequence>(svc);
                    var log = new SqlFace.Models.AP_ACTION_LOG_DBA
                    {
                        LOG_ID = CustomSequence.GetNextVal("AP_ACTION_LOG_DBA_ID", svc),
                        ACTION_BRIEF = null,
                        ACTION_IP = base.GetIp(),
                        ACTION_PAGE = this.Request.RequestUri.AbsolutePath,
                        ACTION_PARAM = String.Format("执行通用处理器{0}-{1},参数={2}", handler.HANDLER_ID, handler.HANDLER_NAME, Newtonsoft.Json.JsonConvert.SerializeObject(context.paramsScaledValues)),
                        ACTION_RESULT = string.Format("开始执行@{0}...", DateTime.Now.ToString()),
                        ACTION_TIME = DateTime.Now,
                        USER_ID = this.User.Identity.Name
                    };

                    var calcMain = new CalcMain(context,new Dictionary<string, string> { { "username", this.User.Identity.Name }, { "ip",base.GetIp()} });
                    try
                    {
                        if (!isSelect && !isGetPagedDataing)
                        {  //非查询

                            var returnstr = calcMain.ExeSqlBlock();
                            if (string.IsNullOrWhiteSpace(returnstr))
                            {
                                // 正常输出
                                outputMsg.msg = "无错误无输出";
                                log.ACTION_RESULT += string.Format("{1},返回信息={0}", returnstr, DateTime.Now.ToString());
                            }
                            else if (Regex.IsMatch(returnstr, "^ORA-[0-9]{4,5}\\b"))
                            {
                                string innerErr = string.Format(",执行中断@{1},发生数据库内部错误={0}", returnstr, DateTime.Now.ToString());
                                throw new ApplicationException(innerErr);
                            }
                            else
                            {   // 正常输出
                                outputMsg.msg = returnstr;
                                log.ACTION_RESULT += string.Format("{1},返回信息={0}", returnstr, DateTime.Now.ToString());
                            }
                            if (!string.IsNullOrWhiteSpace(handler.PREPARING_BLOCK))
                            {
                                //额外数据表输出
                                string extramsg = calcMain.GetQuery();
                                //outputMsg.msg = extramsg;
                                log.ACTION_RESULT += string.Format("匿名块执行成功,执行额外查询结束@{0},结果{1}", DateTime.Now.ToString(), extramsg);
                            }
                        }
                        else //查询
                        {
                            string extramsg = calcMain.GetQuery();
                            outputMsg.msg = extramsg;
                            log.ACTION_RESULT += string.Format(",执行额外查询结束@{0},结果{1}", DateTime.Now.ToString(), extramsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        //捕获异常,for查询类或者非查询类
                        outputMsg.hasError = true;
                        outputMsg.msg = "执行失败!" + ex.Message + "\n" + outputMsg;
                        if (isGetPagedDataing)
                        {
                            log.ACTION_RESULT += "--分页--";
                        }
                        log.ACTION_RESULT += string.Format("执行失败@{0},错误信息={1}", DateTime.Now.ToString(), ex.Message);
                    }
                    finally
                    {
                        if (!isGetPagedDataing)
                            svc.CreateAsync(log);
                    }

                    return Task.FromResult(context);
                }
            }
            else
            {
                throw new ApplicationException("不安全的执行");
            }
        }



        // 
        // POST api/xangular
        //[Route]
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/xangular/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/xangular/5
        //public void Delete(int id)
        //{
        //}

    }


}
