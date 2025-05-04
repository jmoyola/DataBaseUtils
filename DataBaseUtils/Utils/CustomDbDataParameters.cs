using DataBaseUtils.Utils;

namespace DataBaseUtils.Utils;

public class CustomDbDataParameters
{
    private static CustomDbDataParameters _instance = null;
    
    private readonly IDictionary<string, DataHelper.CustomParameterDelegate> _parameters;

    private CustomDbDataParameters(IDictionary<string, DataHelper.CustomParameterDelegate> parameters)
    {
        _parameters = parameters??throw new ArgumentNullException(nameof(parameters));
    }

    public DataHelper.CustomParameterDelegate this[string parameterTypeName] => _parameters[parameterTypeName];

    public static CustomDbDataParameters Instance(IDictionary<string, DataHelper.CustomParameterDelegate> parameters=null)
    {
        if (_instance == null)
        {
            if(parameters==null) throw new ArgumentNullException(nameof(parameters));
            _instance = new CustomDbDataParameters(parameters);
        }
        
        return _instance;
    }

}