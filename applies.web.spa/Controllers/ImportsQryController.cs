using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace EasyPump.Controllers
{
    public class ImportsQryController : ApiController
    {
        public Object Get(string s)
        {
            using (var svc = new SqlFace.Models.OrmService(Config.AppConfigs.sqlfaceconn))
            {
                List<object> ret = new List<object>();
                foreach (var i in svc.FilterWhere<Importing.Imp_Management>(t => t.mem.Contains(s) || t.file_name.Contains(s)))
                {
                    ret.Add(new { mem = i.mem, length = i.length, last_modified_date = i.last_modified_date, file_name = i.file_name, import_id = i.id, total = i.total,addHeads = i.addHeads });
                }
                return ret;
            }
        }
        public Object GetNbr(string nbr)
        {
            using (var svc = new SqlFace.Models.OrmService(Config.AppConfigs.sqlfaceconn))
            {
                List<object> ret = new List<object>();
                foreach (var i in svc.FilterWhere<Importing.Imp_4g_Log>(t => t.phone_number == nbr))
                {
                    var parent = svc.GetById<Importing.Imp_Management>(i.import_id);
                    ret.Add(new { phone_number = i.phone_number, old_pn_level_id = i.old_pn_level_id, pn_level_id = i.pn_level_id, commit_date = i.commit_date, import_id = i.import_id, mem = parent.mem, length = parent.length, file_name = parent.file_name });
                }
                return ret;
            }
        }
        public Object GetImp(string impid)
        {
            using (var svc = new SqlFace.Models.OrmService(Config.AppConfigs.sqlfaceconn))
            {
                List<object> ret = new List<object>();
                var parent = svc.GetById<Importing.Imp_Management>(impid);
                foreach (var i in svc.FilterWhere<Importing.Imp_4g_Log>(t => t.import_id == impid))
                {
                    ret.Add(new { phone_number = i.phone_number, old_pn_level_id = i.old_pn_level_id, pn_level_id = i.pn_level_id, commit_date = i.commit_date, import_id = i.import_id, mem = parent.mem, length = parent.length, file_name = parent.file_name, total = parent.total ,addHeads = parent.addHeads });
                }
                return ret;
            }
        }
    }
}
