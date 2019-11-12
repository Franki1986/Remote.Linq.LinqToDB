using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Aqua.Dynamic;
using Aqua.TypeSystem;
using LinqToDB;
using LinqToDB.Data;
using Remote.Linq.Expressions;
using Remote.Linq.ExpressionVisitors;

namespace Remote.Linq.LinqToDB
{
   public class LinqToDBExpressionExecutor : ExpressionExecutor
   {
      private static readonly Func<Type, System.Reflection.PropertyInfo> TaskResultProperty = (Type resultType) =>
          typeof(Task<>).MakeGenericType(resultType)
              .GetProperty(nameof(Task<object>.Result));

      private static readonly Func<Task, object> GetTaskResult = t => TaskResultProperty(t.GetType().GetGenericArguments().Single()).GetValue(t);

      [SecuritySafeCritical]
      public LinqToDBExpressionExecutor(IDataContext dbContext, ITypeResolver typeResolver = null, IDynamicObjectMapper mapper = null, Func<Type, bool> setTypeInformation = null, Func<System.Linq.Expressions.Expression, bool> canBeEvaluatedLocally = null)
          : this(GetQueryableSetProvider(dbContext), typeResolver, mapper, setTypeInformation, canBeEvaluatedLocally.And(ExpressionEvaluator.CanBeEvaluated))
      {
      }

      public LinqToDBExpressionExecutor(Func<Type, IQueryable> queryableProvider, ITypeResolver typeResolver = null, IDynamicObjectMapper mapper = null, Func<Type, bool> setTypeInformation = null, Func<System.Linq.Expressions.Expression, bool> canBeEvaluatedLocally = null)
          : base(queryableProvider, typeResolver, mapper, setTypeInformation, canBeEvaluatedLocally)
      {
      }

      protected override Expression Prepare(Expression expression) => base.Prepare(expression).ReplaceTableECalls();

      // temporary implementation for compatibility with previous versions
      internal System.Linq.Expressions.Expression PrepareForExecution(Expression expression)
          => Prepare(Transform(Prepare(expression)));

      /// <summary>
      /// Returns the generic <see cref="GetTable{T}"/> for the type specified.
      /// </summary>
      /// <returns>Returns an instance of type <see cref="GetTable{T}"/>.</returns>
      [SecuritySafeCritical]
      private static Func<Type, IQueryable> GetQueryableSetProvider(IDataContext dbContext) => new QueryableSetProvider(dbContext).GetTableMethod;

      [SecuritySafeCritical]
      private sealed class QueryableSetProvider
      {
         private readonly IDataContext _dbContext;

         [SecuritySafeCritical]
         public QueryableSetProvider(IDataContext dbContext)
         {
            _dbContext = dbContext;
         }

         [SecuritySafeCritical]
         public IQueryable GetTableMethod(Type type)
         {
            var getTableMethod = typeof(DataConnection).GetMethod("GetTable", Type.EmptyTypes);
            var q = getTableMethod.MakeGenericMethod(type).Invoke(_dbContext, null);
            return (IQueryable)q;
         }
      }

      private static async Task<object> GetTaskResultAsync(Task task)
      {
         await task.ConfigureAwait(false);
         return GetTaskResult(task);
      }
   }
}
