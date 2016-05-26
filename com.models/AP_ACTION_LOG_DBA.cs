using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    public class AP_ACTION_LOG_DBA : UniqueFilterable
    {
        #region Properties
        public decimal LOG_ID { get; set; }


        public string USER_ID { get; set; }

        public string ACTION_IP { get; set; }


        public string ACTION_PAGE { get; set; }


        public DateTime ACTION_TIME { get; set; }


        public string ACTION_BRIEF { get; set; }


        public string ACTION_PARAM { get; set; }


        public string ACTION_RESULT { get; set; }



        #endregion

        public object GetId()
        {
            return LOG_ID;
        }
    }

}
