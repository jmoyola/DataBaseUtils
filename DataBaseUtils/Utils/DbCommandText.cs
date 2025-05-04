using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace DataBaseUtils.Utils;

public class DbCommandText:Dictionary<string, object>
{
    private string _value;
    private readonly Regex _regexPattern;
    public DbCommandText(string pattern="\\@[a-zA-Z0-9_]+"){}

    public DbCommandText(string value, string pattern="\\@[a-zA-Z0-9_]+")
    {
        Value = value;
        _regexPattern=new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
    }

    public void Add(string key, IDataReader dr, string columnName)
    {
        Add(key, dr.GetValue(dr.GetOrdinal(columnName))); 
    }
    
    public void Add(string key, IDataReader dr, int columnOrdinal)
    {
        Add(key, dr.GetValue(columnOrdinal)); 
    }
    
    public string Value
    {
        get=>_value;
        set => _value = value??throw new ArgumentNullException(nameof(value));
    }

    public string CommandText => GetCommandText();
    
    private string GetCommandText()
    {
        string value=Value;
        
        MatchCollection mc=_regexPattern.Matches(value);
        foreach (var m in mc.Cast<Match>())
        {
            if (ContainsKey(m.Value))
                value = value.Replace(m.Value, this[m.Value]==null?"":this[m.Value].ToSqlValue());
        }

        return value;
    }
    
    public static implicit operator DbCommandText(string value)
    {
        return new DbCommandText(value);
    }
    
    public static implicit operator DbCommandText(StringBuilder value)
    {
        return new DbCommandText(value.ToString());
    }
    
    public static implicit operator string(DbCommandText value)
    {
        return value.CommandText;
    }
}