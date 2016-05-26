using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
namespace SqlFace.Models
{
    public interface IOrmService
    {
        //IDbConnection CreateDbConnection();
        IList<T> GetAll<T>();
        IList<T> GetByIds<T>(IEnumerable ids);
        IList<T> FilterWhere<T>(Expression<Func<T, bool>> q);
        T GetById<T>(object Id);
        void Update<T>(T obj, List<string> fields, Expression<Func<T, bool>> where);
        void Update<TKey,T>(T obj, Expression<Func<T, TKey>> lmd, Expression<Func<T, bool>> where);
        void Delete<T>(T obj, object Keyvalue);
        T Create<T>(T obj);
    }
}
