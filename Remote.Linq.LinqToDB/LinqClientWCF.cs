using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using Aqua.Dynamic;
using LinqToDB.Common;
using Remote.Linq.Expressions;

namespace Remote.Linq.LinqToDB.WCF
{
   public class LinqClient : IDisposable
   {
      private readonly Func<Expression, IEnumerable<DynamicObject>> _dataProvider;
      private readonly Func<Expression, string> _dataProviderSqlInfo;
      private ILinqService _channel;
      private readonly string _configuration;

      private static void ConfigureClientEndpoint(ServiceEndpoint endpoint)
      {
         foreach (OperationDescription operation in endpoint.Contract.Operations)
         {
            DataContractSerializerOperationBehavior dataContractBehavior = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();
            if (dataContractBehavior != null)
            {
               dataContractBehavior.MaxItemsInObjectGraph = Int32.MaxValue;
            }
         }
      }

      private static ILinqService CreateServerConnection(Binding binding, EndpointAddress endpointAddress)
      {
         ChannelFactory<ILinqService> factory = new ChannelFactory<ILinqService>(binding, endpointAddress);
         ConfigureClientEndpoint(factory.Endpoint);
         var channel = factory.CreateChannel();
         return channel;
      }

      public LinqClient(Binding binding, EndpointAddress endpointAddress, string configuration = null)
      {
         _configuration = configuration;
         _dataProvider = expression =>
         {
            _channel = CreateServerConnection(binding, endpointAddress);
            IEnumerable<Aqua.Dynamic.DynamicObject> result = _channel.ExecuteQuery(expression, configuration);
            return result;
         };
      }

      public IQueryable<T> GetTable<T>() where T : class
      {
         var result = RemoteQueryable.Factory.CreateQueryable<T>(_dataProvider) as IQueryable<T>;
         return result;
      }
      
      public void Dispose()
      {
         ICommunicationObject co = _channel as ICommunicationObject;
         if (co != null)
         {
            if (co.State == CommunicationState.Faulted)
            {
               co.Abort();
            }
            else
            {
               co.Close();
            }
         }
      }
   }
}
