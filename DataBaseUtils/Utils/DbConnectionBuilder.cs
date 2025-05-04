using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;

namespace DataBaseUtils.Utils;

[Serializable]
public class DbConnectionBuilderException : Exception
{
    protected DbConnectionBuilderException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public DbConnectionBuilderException(string message) : base(message) { }

    public DbConnectionBuilderException(string message, Exception innerException) : base(message, innerException) { }
}

public class DbConnectionBuilder
{
    public const string DefaultInstanceId="__DEFAULT__";
    
    private static IDictionary<string, DbConnectionBuilder> _instances = new Dictionary<string, DbConnectionBuilder>();
    
    private readonly DbProviderFactory _dbProviderFactory;
    private readonly string _connectionString;

    public DbConnectionBuilder(string dbProviderInvariantName, string sCnx)
    :this(DbProviderFactories.GetFactory(dbProviderInvariantName), sCnx) { }

    public DbConnectionBuilder(DbProviderFactory dbProviderFactory, string sCnx)
    {
        _dbProviderFactory = dbProviderFactory ?? throw new ArgumentNullException(nameof(dbProviderFactory));
        _connectionString=sCnx??throw new ArgumentNullException(nameof(sCnx));
    }
    
    public DbProviderFactory DbProviderFactory
    {
        get => _dbProviderFactory;
    }
    
    public IDbConnection NewConection()
    {
        IDbConnection cnx= _dbProviderFactory.CreateConnection();
        if(cnx==null)
            throw new DbConnectionBuilderException("Can't create connection");
        
        cnx.ConnectionString = _connectionString;
        return cnx;
    }
    
    public static DbConnectionBuilder Instance(DbProviderFactory dbProviderFactory, string sCnx, string instanceId = DefaultInstanceId)
    {
        if (!_instances.ContainsKey(instanceId))
            _instances.Add(instanceId, new DbConnectionBuilder(dbProviderFactory, sCnx));

        return _instances[instanceId];
    }
    
    public static DbConnectionBuilder Instance(string dbProviderInvariantName, string sCnx, string instanceId = DefaultInstanceId)
    {
        if (!_instances.ContainsKey(instanceId))
            _instances.Add(instanceId, new DbConnectionBuilder(dbProviderInvariantName, sCnx));

        return _instances[instanceId];
    }
    
    public static DbConnectionBuilder Instance(string instanceId = DefaultInstanceId)
    {
        if (!_instances.ContainsKey(instanceId))
            throw new DBConcurrencyException($"Instance {instanceId} is not created.");

        return _instances[instanceId];
    } 
}