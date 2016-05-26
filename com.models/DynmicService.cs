using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Oracle;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace SqlFace.Models
{
    //public abstract class Handtemplate{
    //    public abstract IDbConnectionFactory dbFactory;
    //    MethodInfo m;
    //    public object exec() {
    //        using (var db = dbFactory.OpenDbConnection()) { 
    //          return Method 1 
    //        }
    //    }
    //}

    //public interface IDynmicService
    //{
    //    IList<object> Select(string sql, string connstr);
    //    DataTable SelectToTable(string sql, string connstr);
    //    IList<object> SelectFmt(string sql, string connstr, params object[] parameters);
    //    Dictionary<object, object> SelectDict(string sql, string connstr);
    //    Dictionary<object, List<object>> Lookup(string sql, string connstr);
    //    object Scalar(string sql, string connstr);
    //    List<object> SelectCol(string sql, string connstr);
    //    HashSet<object> SelectColDistinct(string sql, string connstr);
    //}

    public class DynmicService 
    {
        
        public static IList<object> Select(string sql, string connstr)
        {
            using (var db = open(connstr))
            {
                return db.Select<object>(sql);
            }
        }

        private static IDbConnection open(string connstr)
        {
            return OraConnector.GetDb(connstr);
        }

        public static IList<object> SelectFmt(string sql, string connstr, params object[] parameters)
        {
            using (var db = open(connstr))
            {
                return db.SelectFmt<object>(sql, parameters);
            }
        }

        public static Dictionary<object, object> SelectDict(string sql, string connstr)
        {
            using (var db = open(connstr))
            {
                return db.Dictionary<object, object>(sql);
            }
        }

        public static Dictionary<object, List<object>> Lookup(string sql, string connstr)
        {
            using (var db = open(connstr))
            {
                return db.Dictionary<object, List<object>>(sql);
            }
        }
    
        public static List<object> SelectCol(string sql, string connstr)
        {
            using (var db = open(connstr))
            {
                return db.Column<object>(sql);
            }
        }

        public static HashSet<object> SelectColDistinct(string sql, string connstr)
        {
            using (var db = open(connstr))
            {
                return db.ColumnDistinct<object>(sql);
            }
        }

        //[Obsolete]
        //public static System.Data.DataTable SelectToTable(string sql, string connstr)
        //{
        //    return DBTool.RunQuery(sql, connstr).Tables[0];
        //}
        public static void GetTableRowsDataCompact(DataTable dtData, out List<string> cols, out List<object[]> data, out List<Type> colTypes)
        {
            cols = new List<string>();
            data = new List<object[]>();
            colTypes = new List<Type>();
            foreach (DataColumn col in dtData.Columns)
            {
                cols.Add(col.ColumnName);
                colTypes.Add(col.DataType);
            }


            int size = dtData.Columns.Count;
            object[] x = new object[size];

            foreach (DataRow dr in dtData.Rows)
            {
                int j = 0;
                foreach (DataColumn col in dtData.Columns)
                {
                    x[j++] = (dr[col]);
                }
                data.Add(x);
                x = new object[size];
            }
        }

        

        public static string RunScalar(string sql, string connstr)
        {
            using (var db = open(connstr))
            {
                return db.Scalar<string>(sql);
            }
            //return DBTool.getStringByRunQuery(sql, conn);

        }
        public static Task<string> RunScalarAsync(string sql, string connstr)
        {
            using (var db = open(connstr))
            {
                return db.ScalarAsync<string>(sql);
            }
            //return DBTool.getStringByRunQuery(sql, conn);

        }
    }

}
