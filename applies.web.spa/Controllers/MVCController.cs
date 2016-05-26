using EasyPump.Config;
using SqlFace.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EasyPump.Controllers
{
    [Authorize]
    public class MVCController : Controller
    {
        // GET: MVC
        public ActionResult Easyhandle()
        {
            if (null == Session["Role"] && User.Identity.IsAuthenticated)
            {
                var role = Utils.DBTool.getStringByRunQuery(string.Format("select wxhf_get_Role('{0}') from dual", User.Identity.Name),AppConfigs.sqlfaceconn);
                Session["Role"] = role.Split(',');
            }
            Dictionary<string, string> querys = formatQuery();
            decimal id = -1;
            if (!querys.ContainsKey("id") || !decimal.TryParse(querys["id"], out id)|| id==-1)
                return HttpNotFound("资源不存在!");

            ViewBag.querys = querys;
            ViewBag.linkOuts = getLinkOut(id);
            object ret = Newtonsoft.Json.JsonConvert.SerializeObject(XAngularController.GetRenders(id, null).Result);
            return View(ret);
        }

        private dynamic getLinkOut(decimal id)
        {
            try
            {
                return DBHelper.getStringByRunQuery(string.Format("select hbwh_oss_admin.f_linkout({0}) from dual ", id), AppConfigs.sqlfaceconn);
            }
            catch(Exception e) {
                return new { error = e.Message }.ToString();
            }
        }

        [AllowAnonymous]
        public ActionResult easygo()
        {
            if (!User.Identity.IsAuthenticated)
                Session["Role"] = null;
            Dictionary<string, string> querys = formatQuery();
            ViewBag.querys = querys;

            string ec;
            decimal id = -1;
            if (querys.TryGetValue("ec", out ec) && !string.IsNullOrWhiteSpace(ec))
            {
                using (var svc = new OrmService(AppConfigs.sqlfaceconn))
                {
                    if (!string.IsNullOrWhiteSpace(ec))
                    {
                        var ee = (svc.FilterWhere<ExternalEntryMap>(s => s.Entry_Code == ec)).FirstOrDefault();
                        if (ee != null)
                            id = ee.Engine_ID;
                    }
                }
            }
            if (id == -1)
                return HttpNotFound("资源不存在!");
            else
            {
                ViewBag.linkOuts = getLinkOut(id);
                object ret = Newtonsoft.Json.JsonConvert.SerializeObject(XAngularController.GetRenders(id, null).Result);
                return View("Easyhandle", ret);
            }

        }

        private Dictionary<string, string> formatQuery()
        {
            var test = Request.QueryString;
            Dictionary<string, string> querys = new Dictionary<string, string>();
            foreach (string item in test.Keys)
            {
                querys.Add(item.ToLower(), test[item]);
            }
            return querys;
        }
    }
}
