using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Oracle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    public class MonoOrmService<T> : IOrmService<T> where T : UniqueFilterable
    {
        private IDbConnectionFactory dbFactory;
        private string connectstring;
        public MonoOrmService(string oracleConnectionString = "oradbConnection")
        {
            
            connectstring = oracleConnectionString;
            var lcon = ConfigurationManager.ConnectionStrings[connectstring].ConnectionString;

            dbFactory = new OrmLiteConnectionFactory(lcon
                 , OracleDialect.Provider); //NOT  OracleOrmLiteDialectProvider.Instance
        }
        [Obsolete]
        public IDbConnection GetDb()
        {
            return dbFactory.OpenDbConnection();
        }



        public IList<T> GetAll()
        {
            //using()
            if (manualRelease)
            {
                return GetDb().Select<T>();
            }
            else
                using (var db = GetDb())
                {
                    return db.Select<T>();
                }
        }

        public T GetById(object Id)
        {
            if (manualRelease)
            {
                return GetDb().SingleById<T>(Id);
            }
            else
                using (var db = GetDb())
                {
                    return db.SingleById<T>(Id);
                }
        }

        public void Update(T obj, List<string> fields, Expression<Func<T, bool>> where)
        {
            SqlExpression<T> ev = OrmLiteConfig.DialectProvider.SqlExpression<T>();
            ev.Update(fields).Where(where);
            if (manualRelease)
            {
                GetDb().UpdateOnly(obj, ev);
            }
            else
                using (var db = GetDb())
                {
                    GetDb().UpdateOnly(obj, ev);
                }
        }

        public void Update<TKey>(T obj, Expression<Func<T, TKey>> lmd, Expression<Func<T, bool>> where)
        {
            SqlExpression<T> ev = OrmLiteConfig.DialectProvider.SqlExpression<T>();
            ev.Update(lmd).Where(where);
            //ev.Where(s => s.GetId() == Keyvalue);
            if (manualRelease)
            {
                GetDb().UpdateOnly(obj, ev);
            }
            else
                using (var db = GetDb())
                {
                    GetDb().UpdateOnly(obj, ev);
                }
        }

        public void Delete(T obj, object Keyvalue)
        {
            if (manualRelease)
            {
                GetDb().Delete<T>(p => p.GetId() == Keyvalue);
            }
            else
                using (var db = GetDb())
                {
                    // db.Update<T>(obj, s => s.UniqueFilter(obj));

                    db.Delete<T>(p => p.GetId() == Keyvalue);
                    // db.Update(obj, s => s.UniqueFilter<EasyHandler>(obj));
                }
        }




        public IList<T> FilterWhere(System.Linq.Expressions.Expression<Func<T, bool>> q)
        {
            if (manualRelease)
            {
                return GetDb().Select(q);
            }
            else
                using (var db = GetDb())
                {
                    //var qry = db.From<T>().Where(q);
                    //var test = db.From<T>().Where(q).ToSelectStatement();
                    return db.Select(q);
                }
        }


        public IList<T> GetByIds(IEnumerable ids)
        {

            if (manualRelease)
            {
                return GetDb().SelectByIds<T>(ids);
            }
            else
                using (var db = GetDb())
                {
                    //var qry = db.From<T>().Where(q);
                    //var test = db.From<T>().Where(q).ToSelectStatement();
                    return db.SelectByIds<T>(ids);
                }
        }




        public T Create(T obj)
        {
            if (manualRelease)
            {
                GetDb().Insert(obj);
            }
            else
                using (var db = GetDb())
                {
                    GetDb().Insert(obj);
                }
            return obj;
        }



        public void tryClose()
        {
            try
            {
                this.iconn.Close();
            }
            catch
            { }
        }

        
    }
}
