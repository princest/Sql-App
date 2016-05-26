using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Oracle;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    public class OraConnector
    {
        public static IDbConnection GetDb(string connectstring)
        {            
            var dbFactory = new OrmLiteConnectionFactory(connectstring
                  , OracleDialect.Provider); //NOT  OracleOrmLiteDialectProvider.Instance
            return dbFactory.OpenDbConnection();
        }

    }
}
