using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    [Alias("GRAPH_MAPPINGS")]
    public class  ParamGraphMap :UniqueFilterable
    {
        #region Properties

        public  decimal SIDE_ID { get; set; }
        public  decimal FROMKEY{ get; set; }
        public  decimal TOKEY { get; set; }
        public  decimal GRAPH_ID { get; set; }

        #endregion


        public object GetId()
        {
            return SIDE_ID;
        }
    }
}
