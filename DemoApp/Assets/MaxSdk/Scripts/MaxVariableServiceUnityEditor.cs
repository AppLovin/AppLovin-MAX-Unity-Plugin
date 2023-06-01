public class MaxVariableServiceUnityEditor
{
    private static readonly MaxVariableServiceUnityEditor _instance = new MaxVariableServiceUnityEditor();

    public static MaxVariableServiceUnityEditor Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Returns the variable value associated with the given key, or false if no mapping of the desired type exists for the given key.
    /// </summary>
    /// <param name="key">The variable name to retrieve the value for.</param>
    public bool GetBoolean(string key, bool defaultValue = false)
    {
        return defaultValue;
    }

    /// <summary>
    /// Returns the variable value associated with the given key, or an empty string if no mapping of the desired type exists for the given key.
    /// </summary>
    /// <param name="key">The variable name to retrieve the value for.</param>
    public string GetString(string key, string defaultValue = "")
    {
        return defaultValue;
    }

    [System.Obsolete("This API has been deprecated. Please use our SDK's initialization callback to retrieve variables instead.")]
    public void LoadVariables() { }
}
