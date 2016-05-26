using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    [Alias("AP_HANDLERS")]
    public class EasyHandler : UniqueFilterable
    {
        #region Properties
        public virtual decimal HANDLER_ID
        {
            get;
            set;
        }

        public virtual string HANDLER_NAME
        {
            get;
            set;
        }

        public virtual decimal CATEGORY_ID
        {
            get;
            set;
        }

        public virtual string DB_TYPE
        {
            get;
            set;
        }

        public virtual string DB_CONNECTION
        {
            get;
            set;
        }

        public virtual string SQL_BLOCK
        {
            get;
            set;
        }

        public virtual string HANDLER_DESC
        {
            get;
            set;
        }

        public virtual DateTime CREATE_TIME
        {
            get;
            set;
        }

        public virtual string SQL_CMD_TYPE
        {
            get;
            set;
        }

        public virtual string AUTHORIZED_USERS
        {
            get;
            set;
        }

        public virtual string CREATEOR
        {
            get;
            set;
        }

        public virtual string STS
        {
            get;
            set;
        }

        public virtual string ABBR
        {
            get;
            set;
        }

        public virtual string ABBRFULL
        {
            get;
            set;
        }

        public virtual string PREPARING_BLOCK
        {
            get;
            set;
        }
        //[Display(Name = "伪列")]

        public virtual string EXTENDS01
        {
            get;
            set;
        }

        #endregion
        [Reference]
        public virtual HandlerExtralConfig HandlerExtralConfig
        {
            get;
            set;
        }


        public object GetId()
        {
            return HANDLER_ID;
        }
        public string getParaedSql()
        {
            if ("SELECT".Equals(SQL_CMD_TYPE))
                return SQL_BLOCK;
            return PREPARING_BLOCK;
        }

    }
    [Alias("HANDLER_EXTRAL_CONFIG")]
    public class HandlerExtralConfig : UniqueFilterable
    {
        //[References(typeof(EasyHandler))]
        public decimal Id { get; set; }
        //[Display(Name = "行首连接")]
        public string Entry_Click_Url { get; set; }
        //[Display(Name = "列宽带比例")]
        //[RegularExpression(@"^(\{(\s*\d+\s*:\s*[0-9.]+\s*,?)*\})?$", ErrorMessage = @"格式不正确,需要JSON格式,'键'表示位置,'值'表示比例")]
        public string Width_Ratio_Array { get; set; }
        //[Display(Name = "列宽度基数,默认40")]
        public int? Width_Base { get; set; }


        public object GetId()
        {
            return Id;
        }
    }

}
