using System.Runtime.InteropServices;

#if UNITY_IOS
public class MaxVariableServiceiOS
{
    private static readonly MaxVariableServiceiOS _instance = new MaxVariableServiceiOS();

    public static MaxVariableServiceiOS Instance
    {
        get { return _instance; }
    }

    [DllImport("__Internal")]
    private static extern void _MaxLoadVariables();

    [DllImport("__Internal")]
    private static extern bool _MaxGetBool(string name, bool defaultValue);

    [DllImport("__Internal")]
    private static extern string _MaxGetString(string name, string defaultValue);

    /// <summary>
    /// Returns the variable value associated with the given key, or false if no mapping of the desired type exists for the given key.
    /// </summary>
    /// <param name="key">The variable name to retrieve the value for.</param>
    public bool GetBoolean(string key, bool defaultValue = false)
    {
        return _MaxGetBool(key, defaultValue);
    }

    /// <summary>
    /// Returns the variable value associated with the given key, or an empty string if no mapping of the desired type exists for the given key.
    /// </summary>
    /// <param name="key">The variable name to retrieve the value for.</param>
    public string GetString(string key, string defaultValue = "")
    {
        return _MaxGetString(key, defaultValue);
    }

    [System.Obsolete("This API has been deprecated. Please use our SDK's initialization callback to retrieve variables instead.")]
    public void LoadVariables()
    {
        _MaxLoadVariables();
    }
}

#endif
