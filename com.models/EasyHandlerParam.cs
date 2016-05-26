using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{

    [Alias("AP_HANDLERS_PARAMS")]
    public class EasyHandlerParam : UniqueFilterable
    {
        #region Properties

        public decimal HANDLER_PARAM_ID { get; set; }

        public decimal HANDLER_ID { get; set; }


        public string PARAM_NAME { get; set; }


        public string PARAM_DTYPE { get; set; }


        public string PARAM_NAME_C { get; set; }


        public string CONTROL_TYPE { get; set; }


        public string CONTROL_DATASOURCE_TYPE { get; set; }

        public string CONTROL_DATASOURCE { get; set; }

        public string CONTROL_DATASOURCE_KEY { get; set; }


        public string CONTROL_DATASOURCE_VALUE { get; set; }


        public string INVISIBLE_ROLES { get; set; }

        public string IS_VISIBLE { get; set; }

        public string IS_REQUIRED { get; set; }

        public string VALID_REGULAR { get; set; }

        public string FORMAT_DESC { get; set; }

        public string PARA_DATA_SIZE { get; set; }

        public decimal ORDER_ID { get; set; }

        public string IS_INTERLINK { get; set; }

        public string INTERLINK_EVENT { get; set; }

        public string DB_CONNECTION { get; set; }


        #endregion

        public object GetId()
        {
            return HANDLER_PARAM_ID;
        }
        public bool ResultIsList()
        {
            ControlType t = (ControlType)Enum.Parse(typeof(ControlType), this.CONTROL_TYPE);
            return t == ControlType.ListBoxMulti || t == ControlType.TextBoxDDL;
        }
    }
}
