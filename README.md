# Remote.Linq.LinqToDB

### Description
A [Remote.Linq](https://github.com/6bee/Remote.Linq) extension for [LinqToDB](https://github.com/linq2db) 
This enables your queries to be executed over a remote service.

### WIP
Not all extensions are implemented yet. All write access operations like update or delete will come later.

### Sample

#### Client

Implement repository class, setting-up server connection and providing the queryable data sets 
```C#
public class RemoteRepository: IDisposable
{
  private readonly Func<Expression, IEnumerable<DynamicObject>> _dataProvider;
  private IQueryService _channel;
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
  
  private static IQueryService CreateServerConnection(Binding binding, EndpointAddress endpointAddress)
  {
    ChannelFactory<IQueryService> factory = new ChannelFactory<IQueryService>(binding, endpointAddress);
    ConfigureClientEndpoint(factory.Endpoint);
    var channel = factory.CreateChannel();
    return channel;
  }
 
  public RemoteRepository(Binding binding, EndpointAddress endpointAddress)
  {
    _dataProvider = expression =>
    {
      _channel = CreateServerConnection(binding, endpointAddress);
      IEnumerable<Aqua.Dynamic.DynamicObject> result = _channel.ExecuteQuery(expression);
      return result;
    };
  }

  public IQueryable<T> GetTable<T>() where T: class
  {
	 var result = RemoteQueryable.Factory.CreateQueryable<T>(_dataProvider) as IQueryable<T>;
	 return result;
  }

  public void Dispose()
  {
    if (_channel != null)
    {
      ICommunicationObject co = _channel as ICommunicationObject;
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
```

Use your repository to compose LINQ query and let the data be retrieved from the backend service
```C#

// Create binding and EndpointAddress based on your needs
var binding = // ...
var endpoint = // ...

using(repository = new RemoteRepository(binding, endpoint)){
    var personsAndCars = repository.GetTable<Person>().LoadWith(p => p.Cars).ToList();// Use LinqToDB LoadWith() extension
    var personAndDriversLicense = repository.GetTable<Person>().Join(db.GetTable<DriverLicense>(), (p, d) => p.Id == d.PersonId, (p, d) => 
      select new {Person = p, License = d} ).ToList(); // Use LinqToDb join extensions
}
```

#### Server

Implement the backend service, handling the client's query expression by applying it to a data source e.g. an ORM

```C#

[ServiceContract]
public interface IQueryService
{
   [OperationContract]
    IEnumerable<DynamicObject> ExecuteQuery(Expression queryExpression);
}

[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
public class QueryService : IQueryService, IDisposable
{
  private IDataContext _context;

  public QueryService()
  {
    _context = new MyDataContext(); // Your DataContext that implements LinqToDB.Data.DataConnection
  }

  public IEnumerable<Aqua.Dynamic.DynamicObject> ExecuteQuery(Expression queryExpression)
  {
    var result = queryExpression.ExecuteWithLinqToDb(_context.QueryableProvider);
    return result;
  }
  
  public void Dispose()
  {
    _context?.Dispose();
  }
}
```
