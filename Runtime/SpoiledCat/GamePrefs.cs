using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static partial class GamePrefs
{
    static partial void InternalGetString(string key, string defaultValue, Action<string> ret);
    static partial void InternalSetString(string key, string value);
    static partial void InternalGetInt(string key, int defaultValue, Action<int> ret);
    static partial void InternalSetInt(string key, int value);
    static partial void InternalGetFloat(string key, float defaultValue, Action<float> ret);
    static partial void InternalSetFloat(string key, float value);

    public static string GetString(string key, string defaultValue = default)
    {
        var ret = defaultValue;
        InternalGetString(key, defaultValue, x => ret = x);
        return ret;
    }

    public static void SetString(string key, string value) => InternalSetString(key, value);

    public static int GetInt(string key, int defaultValue = default)
    {
        var ret = defaultValue;
        InternalGetInt(key, defaultValue, x => ret = x);
        return ret;
    }

    public static void SetInt(string key, int value) => InternalSetInt(key, value);

    public static float GetFloat(string key, float defaultValue = default)
    {
        var ret = defaultValue;
        InternalGetFloat(key, defaultValue, x => ret = x);
        return ret;
    }

    public static void SetFloat(string key, float value) => InternalSetFloat(key, value);
}

[Serializable]
public struct KeyValuePairData
{
    [SerializeField] public string key;
    [SerializeField] public string value;

    public KeyValuePairData(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}

[Serializable]
public class KeyValuePairContainer : Dictionary<string, string>, ISerializationCallbackReceiver
{
    private static CultureInfo cultureInfo;
    [SerializeField] KeyValuePairData[] entries = Array.Empty<KeyValuePairData>();

    public bool Set(string key, int value)
    {
        this[key] = value.ToString();
        return true;
    }

    public bool Set(string key, float value)
    {
        cultureInfo ??= CultureInfo.GetCultureInfo("en-US");
        this[key] = value.ToString(cultureInfo);
        return true;
    }

    public bool Set(string key, string value)
    {
        this[key] = value;
        return true;
    }

    public bool Set(string key, bool value)
    {
        this[key] = value.ToString();
        return true;
    }

    public bool TryGet<T>(string key, out T value, T defaultValue)
    {
        value = defaultValue;
        if (!ContainsKey(key))
            return false;

        cultureInfo ??= CultureInfo.GetCultureInfo("en-US");

        bool ret = false;
        var source = this[key];
        try
        {
            // ReSharper disable AssignmentInConditionalExpression
            value = defaultValue switch
            {
                int => (ret = int.TryParse(source, out var v)) ? (T)(object)v : defaultValue,
                float => (ret = float.TryParse(source, NumberStyles.Float, cultureInfo, out var v)) ? (T)(object)v : defaultValue,
                bool => (ret = bool.TryParse(source, out var v)) ? (T)(object)v : defaultValue,
#pragma warning disable CS0665
                _ when typeof(T) == typeof(string) => (ret = true) ? (T)(object)source : defaultValue,
#pragma warning restore CS0665
                _ => defaultValue
            };
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        return ret;

    }

    public void OnAfterDeserialize()
    {
        Clear();

        for (var i = 0; i < entries.Length; i++)
        {
            Add(entries[i].key, entries[i].value);
        }
    }

    public void OnBeforeSerialize()
    {
        entries = new KeyValuePairData[Count];
        var i = 0;
        foreach (var entry in Keys)
        {
            entries[i++] = new KeyValuePairData(entry, this[entry]);
        }
    }
}

[Serializable]
public class KeyContainer : HashSet<string>, ISerializationCallbackReceiver
{
    [SerializeField] List<string> entries;

    public KeyContainer()
    {}

    public KeyContainer(IEnumerable<string> collection) : base(collection){}

    public void OnAfterDeserialize()
    {
        Clear();
        if (entries != null)
        {
            foreach (string entry in entries)
            {
                Add(entry);
            }
        }
    }

    public void OnBeforeSerialize()
    {
        entries?.Clear();

        if (this.Count > 0)
        {
            entries ??= new List<string>();
            entries.AddRange(this);
        }
    }
}
