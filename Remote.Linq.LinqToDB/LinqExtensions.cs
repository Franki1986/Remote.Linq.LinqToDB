using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;

namespace Remote.Linq.LinqToDB
{
   public static class LinqExtensions
   {
      static readonly MethodInfo _loadWithMethodInfo = MemberHelper.MethodOf(() => LoadWith<int>(null, null)).GetGenericMethodDefinition();

      public static IRemoteQueryable<T> LoadWith<T>(
         this IRemoteQueryable<T> queryable,
         Expression<Func<T, object>> selector)
      {
         if (queryable == null) throw new ArgumentNullException(nameof(queryable));
         if (selector == null) throw new ArgumentNullException(nameof(selector));
         var methodCallExpression = Expression.Call(
            null,
            _loadWithMethodInfo.MakeGenericMethod(typeof(T)),
            new[] { queryable.Expression, Expression.Quote(selector) });
         var newQueryable = queryable.Provider.CreateQuery<T>(methodCallExpression);
         return (IRemoteQueryable<T>)newQueryable;
      }

      static readonly MethodInfo _withTableExpressionMethodInfo = MemberHelper.MethodOf(() => WithTableExpression<int>(null, null)).GetGenericMethodDefinition();

      /// <summary>
      /// Replaces access to a table in generated query with SQL expression.
      /// Example below adds hint to a table. Also see <see cref="With{T}(IRemoteQueryable{T}, string)"/> method.
      /// <code>
      /// var tableWithHint = db.Table.WithTableExpression("{0} {1} with (UpdLock)");
      /// </code>
      /// </summary>
      /// <typeparam name="T">Table record mapping class.</typeparam>
      /// <param name="queryable">remote query source.</param>
      /// <param name="expression">SQL template to use instead of table name. Template supports two parameters:
      /// <para> - {0} original table name;</para>
      /// <para> - {1} table alias.</para>
      /// </param>
      /// <returns>Table-like query source with new table source expression.</returns>
      public static IRemoteQueryable<T> WithTableExpression<T>(this IRemoteQueryable<T> queryable, string expression)
      {
         if (expression == null) throw new ArgumentNullException(nameof(expression));
         var methodCallExpression = Expression.Call(
            null,
            _withTableExpressionMethodInfo.MakeGenericMethod(typeof(T)),
            new[] { queryable.Expression, Expression.Constant(expression) });
         var newQueryable = queryable.Provider.CreateQuery<T>(methodCallExpression);
         return (IRemoteQueryable<T>)newQueryable;
      }

      static readonly MethodInfo _with = MemberHelper.MethodOf(() => With<int>(null, null)).GetGenericMethodDefinition();

      /// <summary>
      /// Adds table hints to a table in generated query.
      /// Also see <see cref="WithTableExpression{T}(IRemoteQueryable{T}, string)"/> method.
      /// <code>
      /// // will produce following SQL code in generated query: table tablealias with(UpdLock)
      /// var tableWithHint = db.Table.With("UpdLock");
      /// </code>
      /// </summary>
      /// <typeparam name="T">Table record mapping class.</typeparam>
      /// <param name="queryable">query source.</param>
      /// <param name="args">SQL text, added to WITH({0}) after table name in generated query.</param>
      /// <returns>Table-like query source with table hints.</returns>
      public static IRemoteQueryable<T> With<T>(this IRemoteQueryable<T> queryable, string args)
      {
         if (args == null) throw new ArgumentNullException(nameof(args));

         var methodCallExpression = Expression.Call(
            null,
            _with.MakeGenericMethod(typeof(T)),
            new[] { queryable.Expression, Expression.Constant(args) });

         var newQueryable = queryable.Provider.CreateQuery<T>(methodCallExpression);
         return (IRemoteQueryable<T>)newQueryable;
      }


      public static IQueryable QueryableProvider(this IDataContext db, Type type)
      {
         var getTableMethod = typeof(DataConnection).GetMethod("GetTable", Type.EmptyTypes);
         return (IQueryable)getTableMethod.MakeGenericMethod(type).Invoke(db, null);
      }
   }
}
