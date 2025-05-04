using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;

namespace DataBaseUtils.Utils;

public class PropertyMappings<T>:IEnumerable<PropertyMapping>
{
    private readonly IEnumerable<PropertyMapping> _mappings;

    public PropertyMappings(IEnumerable<string> excluding=null, IEnumerable<string> including=null)
    {
        _mappings = GetMappings(typeof(T), excluding, including);
    }
    
    public IEnumerator<PropertyMapping> GetEnumerator()
    {
        return _mappings.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public static IEnumerable<PropertyMapping> GetMappings(Type type, IEnumerable<string> excluding=null, IEnumerable<string> including=null)
    {
        IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                                  BindingFlags.FlattenHierarchy | BindingFlags.SetProperty);
        
        properties=properties.Where(p => !excluding?.Contains(p.Name) ?? false);
        properties=properties.Where(p => including?.Contains(p.Name) ?? false);
        
        return properties.Select(p=>new PropertyMapping(p, p.Name));
    }
}

public class PropertyMapping
{
    private readonly PropertyInfo _property;
    private readonly string _column;

    public PropertyMapping(PropertyInfo property, string column)
    {
        _property = property??throw new ArgumentNullException(nameof(property));
        _column = column??throw new ArgumentNullException(nameof(column));
    }
    
    public PropertyInfo Property => _property;
    public string Column => _column;

    public override string ToString()
    {
        return $"{_property.Name} => (Db) {_column}";
    }
}



public static class DataHelper
{
    public static IDbDataParameter GetParameter(this IDbCommand cmd, string name)
    {
        return (IDbDataParameter)cmd.Parameters[name];
    }

    public static IDbDataParameter GetParameter(this IDbCommand cmd, int index)
    {
        return (IDbDataParameter)cmd.Parameters[index];
    }

    public static T GetParameterValue<T>(this IDbCommand cmd, string name)
    {
        return (T)((IDbDataParameter)cmd.Parameters[name]).Value;
    }

    public static T GetParameterValue <T>(this IDbCommand cmd, int index)
    {
        return (T)((IDbDataParameter)cmd.Parameters[index]).Value;
    }

    public static IDbCommand AddInputParameter(this IDbCommand cmd, string name, DbType dbType, Object value, int size = -1)
    {
        return AddParameter(cmd, ParameterDirection.Input, name, dbType, value, size);
    }

    public static IDbCommand AddOutputParameter(this IDbCommand cmd, string name, DbType dbType, int size = -1)
    {
        return AddParameter(cmd, ParameterDirection.Output, name, dbType, null, size);
    }

    public static IDbCommand AddInputOutputParameter(this IDbCommand cmd, string name, DbType dbType, Object value, int size = -1)
    {
        return AddParameter(cmd, ParameterDirection.InputOutput, name, dbType, value, size);
    }

    public delegate IDbDataParameter CustomParameterDelegate(IDbCommand cmd);
    
    public static IDbCommand AddCustomParameter(this IDbCommand cmd, string name, ParameterDirection direction, CustomParameterDelegate customParameterDelegate)
    {
        IDbDataParameter p = customParameterDelegate(cmd);
        p.ParameterName = name;
        p.Direction=direction;
        cmd.Parameters.Add(p);
        return cmd;
    }

    public static IDbCommand AddCustomParameter(this IDbCommand cmd, string name, ParameterDirection direction, string parameterTypeName)
    {
        return AddCustomParameter(cmd, name, direction, CustomDbDataParameters.Instance()[parameterTypeName]);
    }

    public static IDbCommand AddParameter(this IDbCommand cmd, ParameterDirection direction, string name, DbType dbType, Object value, int size=-1)
    {
        IDbDataParameter prm = cmd.CreateParameter();
        
        prm.ParameterName = name;
        prm.Direction = direction;        
        prm.DbType = dbType;

        if (value != null)
        {
            if (value is bool bValue)
                prm.Value = (bValue ? -1 : 0);
            else
                prm.Value = value;
        }

        if(size>-1)
            prm.Size = size;
        

        cmd.Parameters.Add(prm);
        
        return cmd;
    }

    public static Object SelectScalar(this IDbConnection cnx, string command, bool closeConnection=false)
    {
        if (cnx == null) throw new ArgumentNullException(nameof(cnx));
        
        try
        {
            if(cnx.State!=ConnectionState.Open)
                cnx.Open();
            
            IDbCommand cmd = cnx.CreateCommand();
            cmd.CommandText = command;
            Object oRet = cmd.ExecuteScalar();
            if(DBNull.Value.Equals(oRet))
                return null;
            else
                return oRet;
        }
        finally
        {
            if(closeConnection && cnx.State!= ConnectionState.Closed)
                cnx.Close();
        }
    }

    public static T? SelectScalar<T>(this IDbConnection cnx, string command, bool closeConnection=false) where T:struct
    {
        Object oRet = SelectScalar(cnx, command, closeConnection);
        if(DBNull.Value.Equals(oRet))
            return null;
        
        return (T?)oRet;
    }
    
    public static IList<T> SelectScalar<T>(this IDbConnection cnx, string command, string columnName, bool closeConnection = false)
    {
        return Select(cnx, command, closeConnection).Rows.Cast<DataRow>().ToList()
            .Select(v=>v[columnName])
            .Cast<T>().ToList();
    }

    public static IList<DataRow> SelectRows(this IDbConnection cnx, string command, bool closeConnection = false)
    {
        return Select(cnx, command, closeConnection).Rows.Cast<DataRow>().ToList();
    }
    
    public static DataTable Select(this IDbConnection cnx, string command, bool closeConnection=false)
    {
        DataTable ret = new DataTable();
        if (cnx == null) throw new ArgumentNullException(nameof(cnx));
        
        try
        {
            if(cnx.State!=ConnectionState.Open)
                cnx.Open();
            
            IDbCommand cmd = cnx.CreateCommand();
            cmd.CommandText = command;
            IDataReader reader = cmd.ExecuteReader();
            ret.Load(reader);

            return ret;
        }
        finally
        {
            if(closeConnection && cnx.State!= ConnectionState.Closed)
                cnx.Close();
        }
    }

    public static int Ddl(this IDbConnection cnx, string command, bool closeConnection=false)
    {
        if (cnx == null) throw new ArgumentNullException(nameof(cnx));
        
        try
        {
            if(cnx.State!=ConnectionState.Open)
                cnx.Open();
            
            IDbCommand cmd = cnx.CreateCommand();
            cmd.CommandText = command;
            return cmd.ExecuteNonQuery();
        }
        finally
        {
            if(closeConnection && cnx.State!= ConnectionState.Closed)
                cnx.Close();
        }
    }
    
    public static int Execute(this IDbCommand cmd, bool closeConnection=false)
    {
        if (cmd == null) throw new ArgumentNullException(nameof(cmd));
        
        try
        {
            if(cmd.Connection.State!=ConnectionState.Open)
                cmd.Connection.Open();
            
            return cmd.ExecuteNonQuery();
        }
        finally
        {
            if(closeConnection && cmd.Connection.State!= ConnectionState.Closed)
                cmd.Connection.Close();
        }
    }
    
    public static IList<T> ToList<T>(DataTable table, PropertyMappings<T> mappings)
    {
        IList<T> ret = new List<T>();

        foreach (var row in table.Rows.Cast<DataRow>())
        {
            object oItem = Activator.CreateInstance<T>();
            foreach (var mapping in mappings)
            {
                object v=row[mapping.Column];
                v = DBNull.Value.Equals(v) ? null : v;
                mapping.Property.SetValue(oItem, v);
            }
        }
        
        return ret;
    }

    public static IDictionary<string, object> ToDictionary(this DataRow dataRow)
    {
        Dictionary<string, object> ret = new Dictionary<string, object>();

        foreach (DataColumn column in dataRow.Table.Columns.Cast<DataColumn>())
            ret.Add(column.ColumnName, dataRow[column.Ordinal]);
        
        return ret;
    }

    public static IList<IDictionary<string, object>> ToList(this IDataReader dataRow)
    {
        IList<IDictionary<string, object>> lRet = new List<IDictionary<string, object>>();

        while (dataRow.Read())
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            for (int i=0; i< dataRow.FieldCount;i++)
                ret.Add(dataRow.GetName(i), dataRow.GetValue(i));
            lRet.Add(ret);
        }

        return lRet;
    }
    
    public static IDictionary<string, object> ToDictionary(this IDataReader dataRow)
    {
        Dictionary<string, object> ret = new Dictionary<string, object>();
        
        for (int i=0; i< dataRow.FieldCount;i++)
            ret.Add(dataRow.GetName(i), dataRow.GetValue(i));

        return ret;
    }
    
    public static string ToSqlValue(this object value)
    {
        return SqlValue(value);
    }
    
    public static string SqlValue(object value)
    {
        if (value == null || DBNull.Value.Equals(value))
            return "NULL";
        if (value is string)
            return $"'{value}'";
        if (value is bool b)
            return b?"-1":"0";
        if (value is DateTime time)
            return $"TIMESTAMP '{time:yyyy-MM-dd HH:mm:ss.fff}'";
        if (value is TimeSpan timeSpan)
            return $"INTERVAL '{timeSpan:dd HH:mm:ss.fff}'";
        
        if (value is float || value is double || value is decimal)
            return $"{((decimal)value).ToString(CultureInfo.InvariantCulture.NumberFormat)}";

        return $"{value}";
    }
    
    public static T Get<T>(this IDataReader value, string columnName)
    {
        return (T)value[columnName];
    }
}