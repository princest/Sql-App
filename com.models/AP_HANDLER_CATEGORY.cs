using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    class AP_HANDLER_CATEGORY
    {
        #region Properties

        public  decimal CATEGORY_ID{get;set;}
         

        public  string CATEGORY_NAME{get;set;}
        

        public  decimal DISPLAY_ORDER{get;set;}
         

        public  decimal DISPLAY_ON_MENU{get;set;}


        public  decimal PARENT_ID { get; set; }
         

        #endregion
    }
}
