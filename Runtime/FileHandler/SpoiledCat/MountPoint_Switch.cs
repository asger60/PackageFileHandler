#if UNITY_SWITCH && !UNITY_EDITOR
#define TT_UNITY_SWITCH
#endif

#if TT_UNITY_SWITCH

using System;
using System.Collections.Generic;
using System.IO;
using SpoiledCat.SimpleIO;
using UnityEngine;
using nn.account;
using UnityEngine.Switch;

public static partial class MountPoint
{
    private static Dictionary<SPath, byte[]> saveRequests = new Dictionary<SPath, byte[]>();
    private static List<SPath> saved = new List<SPath>();
    private static float NextSaveTimestamp = 0f;
    private static long totalSaveSize = 0;
    private static int totalSaveCount = 0;
    const string Prefix = "rytmos";
    const long MAX_SAVE_SIZE = 14 * 1024 * 1024;
    const int MAX_SAVE_WRITES = 28;

    static MountPoint()
    {
        Mount(Prefix);
    }

    private static bool Mount(string mountPoint)
    {
        var ret = nn.fs.SaveData.Mount(mountPoint, GameUser.Instance.UserID).IsSuccess();
        if (ret)
        {
            nn.fs.SaveData.Ensure(GameUser.Instance.UserID);

            Notification.notificationMessageReceived += Notification_notificationMessageReceived;
            Notification.EnterExitRequestHandlingSection();
        }
        return ret;
    }

    static SPath InternalGetFilePath(string filename)
    {
        return $"{Prefix}:/{filename}".ToSPath();
    }

    static byte[] InternalLoad(SPath file)
    {
        if (saveRequests.ContainsKey(file))
            return saveRequests[file];
        return file.ReadAllBytes();
    }

    static void InternalSave(SPath file, byte[] bytes, long length)
    {
        if (!saveRequests.ContainsKey(file))
            saveRequests.Add(file, bytes);
        else
            saveRequests[file] = bytes;

        Flush();
    }

    static void Flush(bool force = false)
    {
        if (!force && UnityEngine.Time.realtimeSinceStartup < NextSaveTimestamp)
        {
#if DEBUG
            Debug.Log($"#SwitchSave# Gating the save because the last one was {NextSaveTimestamp - UnityEngine.Time.realtimeSinceStartup} seconds ago");
#endif
            return;
        }
#if DEBUG
        else
        {
            Debug.Log($"#SwitchSave# Saving. Previous save was {NextSaveTimestamp - UnityEngine.Time.realtimeSinceStartup} seconds ago");
        }
#endif

        // Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        totalSaveSize = 0;
        totalSaveCount = 0;

        foreach (var request in saveRequests)
        {
            var length = request.Value.Length;
            totalSaveSize += length;
            totalSaveCount++;

            Debug.Log($"#SwitchSave# Saving {request.Key} {force} {totalSaveCount} {totalSaveSize}");

            if (!force && (totalSaveSize >= MAX_SAVE_SIZE || totalSaveCount > MAX_SAVE_WRITES))
            {
                break;
            }

            request.Key.WriteAllBytes(request.Value, length);
            saved.Add(request.Key);
        }

        foreach (var key in saved)
        {
            saveRequests.Remove(key);
        }
        saved.Clear();

        // we didn't write everything because we went over the limit, use a bigger timeout
        if (saveRequests.Count > 0)
        {
            NextSaveTimestamp = UnityEngine.Time.realtimeSinceStartup + 60;
        }
        else
        {
            NextSaveTimestamp = UnityEngine.Time.realtimeSinceStartup + 5;
        }

        Debug.Log($"#SwitchSave# Next save {NextSaveTimestamp}");

        // Nintendo Switch Guideline 0080
        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
    }

    private static void Notification_notificationMessageReceived(Notification.Message message)
    {
        if (message == Notification.Message.ExitRequest)
        {
            Flush(true);
            Notification.LeaveExitRequestHandlingSection();
        }
    }
}

#endif