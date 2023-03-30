#if !UNITY_SWITCH && !UNITY_EDITOR

using System;
using UnityEngine;

public static partial class GamePrefs
{
	static partial void InternalGetString(string key, string defaultValue, Action<string> ret)
	{
		ret(PlayerPrefs.GetString(key, defaultValue));
	}

	static partial void InternalSetString(string key, string value)
	{
		PlayerPrefs.SetString(key, value);
	}

	static partial void InternalGetInt(string key, int defaultValue, Action<int> ret)
	{
		ret(PlayerPrefs.GetInt(key, defaultValue));
	}

	static partial void InternalSetInt(string key, int value)
	{
		PlayerPrefs.SetInt(key, value);
	}

	static partial void InternalGetFloat(string key, float defaultValue, Action<float> ret)
	{
		ret(PlayerPrefs.GetFloat(key, defaultValue));
	}
	static partial void InternalSetFloat(string key, float value)
	{
		PlayerPrefs.SetFloat(key, value);
	}
}

#endif
