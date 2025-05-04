namespace DataBaseUtils.Utils;

public abstract class DataObject
{
    private DbConnectionBuilder _connectionBuilder;
    public event EventHandler<string> Logging;
    
    public DbConnectionBuilder ConnectionBuilder
    {
        get => _connectionBuilder;
        set => _connectionBuilder=value;
    }

    protected void Log(string message) => OnLoggin(message);

    protected void OnLoggin(string message)
    {
        Logging?.Invoke(this, message);
    }

}