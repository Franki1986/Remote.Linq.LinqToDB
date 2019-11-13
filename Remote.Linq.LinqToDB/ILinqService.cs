using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace Remote.Linq.LinqToDB
{
   [ServiceContract]
   public interface ILinqService
   {
      [OperationContract]
      IEnumerable<Aqua.Dynamic.DynamicObject> ExecuteQuery(Remote.Linq.Expressions.Expression queryExpression, string configuration = null);

      [OperationContract]
      int Insert(Aqua.Dynamic.DynamicObject obj, string tableName = null, string databaseName = null, string schemaName = null, string serverName = null,  string configuration = null);
   }
}