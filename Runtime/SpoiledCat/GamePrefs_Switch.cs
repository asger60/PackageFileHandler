#if UNITY_SWITCH && !UNITY_EDITOR

using SpoiledCat.SimpleIO;
using System;
using UnityEngine;

public static partial class GamePrefs
{
    private static GameUser user;

    private const string prefsName = "playerprefs.json";
    private static SPath prefsFile;
    private static KeyValuePairContainer prefs = new KeyValuePairContainer();

    static GamePrefs()
    {
        user = GameUser.Instance;
        prefsFile = MountPoint.GetFilepath(prefsName);
        if (prefsFile.FileExists())
        {
            Load();
        }
    }

    static partial void InternalGetString(string key, string defaultValue, Action<string> ret)
    {
        prefs.TryGet(key, out var val, defaultValue);
        ret(val);
    }

    static partial void InternalSetString(string key, string value)
    {
        prefs.Set(key, value);
        Save();
    }

    static partial void InternalGetInt(string key, int defaultValue, Action<int> ret)
    {
        prefs.TryGet(key, out var val, defaultValue);
        ret(val);
    }

    static partial void InternalSetInt(string key, int value)
    {
        prefs.Set(key, value);
        Save();
    }

    static partial void InternalGetFloat(string key, float defaultValue, Action<float> ret)
    {
        prefs.TryGet(key, out var val, defaultValue);
        ret(val);
    }

    static partial void InternalSetFloat(string key, float value)
    {
        prefs.Set(key, value);
        Save();
    }

    private static void Load()
    {
        string data = null;
        try
        {
            data = prefsFile.ReadAllText();
            prefs = JsonUtility.FromJson<KeyValuePairContainer>(data);
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.LogFormat("json: {0} ex: {1}", data, ex);
#endif
        }
    }

    private static void Save()
    {
        prefsFile.WriteAllText(JsonUtility.ToJson(prefs));
    }
}

#endif