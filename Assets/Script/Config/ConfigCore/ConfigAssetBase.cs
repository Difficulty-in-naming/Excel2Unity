using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ConfigAssetBase<T> : ScriptableObject where T : class, new()
{
    [SerializeField] private List<T> mConfig = new List<T>();
    public List<T> Config
    {
        get { return mConfig; }
    }

    private static T mInstance;
    public static T Instance
    {
        get
        {
            var t = typeof(T);
            var path = (string)(t.GetField("Path", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
            return (T) (object) Resources.Load(path,t);
        }
    }
}