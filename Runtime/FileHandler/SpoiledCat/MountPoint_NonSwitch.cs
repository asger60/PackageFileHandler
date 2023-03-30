#if UNITY_SWITCH && !UNITY_EDITOR
#define TT_UNITY_SWITCH
#endif

#if !TT_UNITY_SWITCH

using System.IO;
using SpoiledCat.SimpleIO;
using UnityEngine;

public static partial class MountPoint
{
    static SPath InternalGetFilePath(string filename) => Application.persistentDataPath.ToSPath().Combine(filename);
    static byte[] InternalLoad(SPath file) => file.ReadAllBytes();
    static void InternalSave(SPath file, byte[] bytes, long length) => file.WriteAllBytes(bytes, length);
}

#endif
