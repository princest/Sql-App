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
    //public delegate T DelegateInvoker<T>(T a);
    //DelegateInvoker<T> a = Create;
    //a.BeginInvoke(new T(), test, 1);
    public class OrmService : IDisposable
    {
        private IDbConnection db;
        public OrmService(string dbc)
        {
            //LogManager.LogFactory = new ConsoleLogFactory();
            //SqlServerOrmLiteDialectProvider.Instance);//IOrmLiteDialectProvider
            //dbFactory.DialectProvider = new 
            //IDbConnection dbConn = dbFactory.OpenDbConnection();
            //IDbCommand dbCmd = dbConn.CreateCommand();
            //dbConn.DropTable<Employee>();
            //dbConn.CreateTable<Employee>();
            //service = dbFactory.OpenDbConnection();
            //this.db = connector.GetDb();
            this.db = OraConnector.GetDb(dbc);
        }
        public IDbConnection theConnection { get { return db; } private set { db = value; } }
        public void Dispose()
        {
            db.Dispose();
        }
        public IList<T> GetAll<T>()
        {
            return db.Select<T>();
        }
        public Task<List<T>> GetAllAsync<T>() where T : new()
        {
            return db.SelectAsync<T>();
        }

        public T GetById<T>(object Id)
        {
            return db.SingleById<T>(Id);
        }
        public Task<T> GetByIdAsync<T>(object Id)
        {
            return db.SingleByIdAsync<T>(Id);
        }
        public void Update<T>(T obj, List<string> fields, Expression<Func<T, bool>> where)
        {
            SqlExpression<T> ev = OrmLiteConfig.DialectProvider.SqlExpression<T>();
            ev.Update(fields).Where(where);
            db.UpdateOnly(obj, ev);
        }

        public Task UpdateAsync<T>(T obj, List<string> fields, Expression<Func<T, bool>> where)
        {
            SqlExpression<T> ev = OrmLiteConfig.DialectProvider.SqlExpression<T>();
            ev.Update(fields).Where(where);
            return db.UpdateOnlyAsync(obj, ev);
        }

        public void Update<T, TKey>(T obj, Expression<Func<T, TKey>> lmd, Expression<Func<T, bool>> where)
        {
            SqlExpression<T> ev = OrmLiteConfig.DialectProvider.SqlExpression<T>();
            ev.Update(lmd).Where(where);
            //ev.Where(s => s.GetId() == Keyvalue);
            db.UpdateOnly(obj, ev);

        }

        public Task UpdateAsync<T, TKey>(T obj, Expression<Func<T, TKey>> lmd, Expression<Func<T, bool>> where)
        {
            SqlExpression<T> ev = OrmLiteConfig.DialectProvider.SqlExpression<T>();
            ev.Update(lmd).Where(where);
            //ev.Where(s => s.GetId() == Keyvalue);
            return db.UpdateOnlyAsync(obj, ev);

        }
        public void Delete<T>(T obj, object Keyvalue) where T : UniqueFilterable
        {
            db.Delete<T>(p => p.GetId() == Keyvalue);
        }
        public Task DeleteAsync<T>(T obj, object Keyvalue) where T : UniqueFilterable
        {
            return db.DeleteAsync<T>(p => p.GetId() == Keyvalue);
        }
        public IList<T> FilterWhere<T>(System.Linq.Expressions.Expression<Func<T, bool>> q)
        {
            return db.Select(q);
        }
        public Task<List<T>> FilterWhereAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> q)
        {
            return db.SelectAsync(q);
        }

        public IList<T> GetByIds<T>(IEnumerable ids)
        {
            return db.SelectByIds<T>(ids);
        }

        public Task<List<T>> GetByIdsAsync<T>(IEnumerable ids)
        {
            return db.SelectByIdsAsync<T>(ids);
        }


        public T Create<T>(T obj)
        {
            db.Insert(obj);
            return obj;
        }
        public Task<T> CreateAsync<T>(T obj)
        {
            db.InsertAsync(obj);
            return Task.FromResult<T>(obj);
        }
    }
}
