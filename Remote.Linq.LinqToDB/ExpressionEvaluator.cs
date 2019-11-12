namespace Remote.Linq.LinqToDB
{
   using System.Linq.Expressions;

   public static class ExpressionEvaluator
   {
      public static bool CanBeEvaluated(Expression expression)
      {
         //  if ((expression as MemberExpression)?.Member.DeclaringType == typeof(EF))
         //{
            //   return false;
         //}
         // todo: what type should be returned true or false?
         return true;
      }
   }
}
