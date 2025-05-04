using System.Data.Common;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace DataBaseUtils.Utils;

[Serializable]
public class DbProviderFactoriesException : Exception
{
    protected DbProviderFactoriesException(SerializationInfo info, StreamingContext context):base(info,context){}
    public DbProviderFactoriesException(string message, Exception innerException) : base(message, innerException) { }
    public DbProviderFactoriesException(string message) : base(message) { }
}

public static class DbProviderFactories
{
    private static readonly IDictionary<string, string> Factories=new Dictionary<string, string>();
    
    public static DbProviderFactory GetFactory(string dbProviderFactoryTypename, string assemblyName)
    {
        try
        {
            Assembly asm = Assembly.Load(assemblyName);

            Type t = asm.GetType(dbProviderFactoryTypename);
            if(!typeof(DbProviderFactory).IsAssignableFrom(t))
                throw new InvalidCastException($"{dbProviderFactoryTypename} in {assemblyName} is not assignable form DbProviderFactory");
            
            var pi = t.GetProperty("Instance", BindingFlags.Public| BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.Instance);

            DbProviderFactory instance=(DbProviderFactory)pi?.GetValue(null);
            
            if (instance == null)
                throw new DbProviderFactoriesException("DbProviderFactory instance is null");

            return instance;
        }
        catch (Exception ex)
        {
            throw new DbProviderFactoriesException("Error getting db provider factory", ex);
        }
    }
    
    public static DbProviderFactory GetFactory(string dbProviderInvariantName)
    {
        if(!Factories.ContainsKey(dbProviderInvariantName))
            throw new DbProviderFactoriesException($"Can't find instance for {dbProviderInvariantName}");

        return GetFactoryFull(Factories[dbProviderInvariantName]);
    }

    public static void AddFactory(string dbProviderInvariantName, string factoryTypeAssemblyQualifiedName)
    {
        Factories.Add(dbProviderInvariantName, factoryTypeAssemblyQualifiedName);
    }

    public static IEnumerable<KeyValuePair<string, string>> Available()
    {
        return Factories.ToList().AsReadOnly();
    }

    private static readonly Regex FactoryTypeAssemblyQualifiedNameRegex = new Regex(@"^\s*[^,]*\s*,\s*.*\s*$");
    private static DbProviderFactory GetFactoryFull(string factoryTypeAssemblyQualifiedName)
    {
        var m = FactoryTypeAssemblyQualifiedNameRegex.Match(factoryTypeAssemblyQualifiedName);
        
        if (!m.Success) throw new DbProviderFactoriesException($"'{factoryTypeAssemblyQualifiedName}' is malformed.");
        
        return GetFactory(m.Groups[1].Value, m.Groups[2].Value);
    }
}