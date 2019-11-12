using Remote.Linq.Expressions;
using Remote.Linq.ExpressionVisitors;

namespace Remote.Linq.LinqToDB
{
   using System.ComponentModel;
   using System.Linq;

   [EditorBrowsable(EditorBrowsableState.Never)]
   public static class RemoteExpressionReWriter
   {
      private static readonly System.Reflection.MethodInfo _loadWithMethodInfo = typeof(LinqExtensions)
         .GetMethods()
         .Single(x => x.Name == "LoadWith");

      /// <summary>
      /// Replaces resource descriptors by queryable and replaces include method call with entity framework's include methods.
      /// </summary>
      internal static Expression ReplaceTableECalls(this Expression expression)
      {
         return new ElementReplacer().Run(expression);
      }

      private sealed class ElementReplacer : RemoteExpressionVisitorBase
      {
         internal ElementReplacer()
         {
         }

         internal Expression Run(Expression expression)
         {
            var result = Visit(expression);
            return result;
         }


         protected override Expression VisitMethodCall(MethodCallExpression expression)
         {
            if (expression.Method.Name == "LoadWith" &&
                expression.Method.DeclaringType.Type == typeof(LinqExtensions) &&
                expression.Method.GenericArgumentTypes.Count == 1 &&
                expression.Arguments.Count == 2)
            {
               var elementType = expression.Method.GenericArgumentTypes.Single().Type;

               var queryableExpression = expression.Arguments[0];
               var pathExpression = expression.Arguments[1];
               var efIncludeMethod = _loadWithMethodInfo.MakeGenericMethod(elementType);
               var callExpression = new MethodCallExpression(null, efIncludeMethod, new[] { queryableExpression, pathExpression });
               expression = callExpression;
            }

            return base.VisitMethodCall(expression);
         }
      }
   }
}
