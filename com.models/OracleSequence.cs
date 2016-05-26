using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    [Alias("AP_SEQ")]
    public class CustomSequence:UniqueFilterable
    {
        #region Properties

        private static object _syncObj = new  object();
        public string SEQ_NAME
        {
            get;
            set;
        }

        public decimal SEQ_NO
        {
            get;
            set;
        }


        #endregion

        public object GetId()
        {
            return SEQ_NAME;
        }
        public static decimal GetNextVal(string sname, OrmService svc)
        {
            decimal dNextVal = -1;
            CustomSequence seq;
            //var service = new OrmService<CustomSequence>(db);
            lock (_syncObj)
            {
                seq = svc.GetById<CustomSequence>(sname);
                if (null!= seq)
                {
                    dNextVal = seq.SEQ_NO + 1;
                    seq.SEQ_NO = dNextVal;
                    svc.Update(seq,f=>f.SEQ_NO,s=>s.SEQ_NAME==sname);
                }
                else
                {
                    dNextVal = 1;
                    svc.Create(new CustomSequence { SEQ_NAME = sname, SEQ_NO = 1 });
                }
            }
            return dNextVal;
        }
    }
}
