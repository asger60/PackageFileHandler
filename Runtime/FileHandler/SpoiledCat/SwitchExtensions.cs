#if UNITY_SWITCH

using System;

namespace SpoiledCat.SimpleIO
{
	static partial class SwitchExtensions
	{
		public static bool IsPathNotFound(this nn.Result result) => nn.fs.FileSystem.ResultPathNotFound.Includes(result);
		public static bool IsPathAlreadyExists(this nn.Result result) => nn.fs.FileSystem.ResultPathAlreadyExists.Includes(result);
		public static bool IsTargetLocked(this nn.Result result) => nn.fs.FileSystem.ResultTargetLocked.Includes(result);
		public static bool IsDirectoryNotEmpty(this nn.Result result) => nn.fs.FileSystem.ResultDirectoryNotEmpty.Includes(result);
		public static bool IsDirectoryStatusChanged(this nn.Result result) => nn.fs.FileSystem.ResultDirectoryStatusChanged.Includes(result);
		public static bool IsUsableSpaceNotEnough(this nn.Result result) => nn.fs.FileSystem.ResultUsableSpaceNotEnough.Includes(result);
		public static bool IsUnsupportedSdkVersion(this nn.Result result) => nn.fs.FileSystem.ResultUnsupportedSdkVersion.Includes(result);
		public static bool IsMountNameAlreadyExists(this nn.Result result) => nn.fs.FileSystem.ResultMountNameAlreadyExists.Includes(result);
		public static bool IsTargetNotFound(this nn.Result result) => nn.fs.FileSystem.ResultTargetNotFound.Includes(result);
		public static bool IsUsableSpaceNotEnoughForSaveData(this nn.Result result) => nn.fs.SaveData.ResultUsableSpaceNotEnoughForSaveData.Includes(result);
		public static bool IsSaveDataHostFileSystemCorrupted(this nn.Result result) => nn.fs.Host.ResultSaveDataHostFileSystemCorrupted.Includes(result);
		public static bool IsSaveDataHostEntryCorrupted(this nn.Result result) => nn.fs.Host.ResultSaveDataHostEntryCorrupted.Includes(result);
		public static bool IsSaveDataHostFileDataCorrupted(this nn.Result result) => nn.fs.Host.ResultSaveDataHostFileDataCorrupted.Includes(result);
		public static bool IsSaveDataHostFileCorrupted(this nn.Result result) => nn.fs.Host.ResultSaveDataHostFileCorrupted.Includes(result);
		public static bool IsInvalidSaveDataHostHandle(this nn.Result result) => nn.fs.Host.ResultInvalidSaveDataHostHandle.Includes(result);
		public static bool IsHostFileSystemCorrupted(this nn.Result result) => nn.fs.Host.ResultHostFileSystemCorrupted.Includes(result);
		public static bool IsHostEntryCorrupted(this nn.Result result) => nn.fs.Host.ResultHostEntryCorrupted.Includes(result);
		public static bool IsHostFileDataCorrupted(this nn.Result result) => nn.fs.Host.ResultHostFileDataCorrupted.Includes(result);
		public static bool IsHostFileCorrupted(this nn.Result result) => nn.fs.Host.ResultHostFileCorrupted.Includes(result);
		public static bool IsInvalidHostHandle(this nn.Result result) => nn.fs.Host.ResultInvalidHostHandle.Includes(result);
		public static bool IsRomHostFileSystemCorrupted(this nn.Result result) => nn.fs.Rom.ResultRomHostFileSystemCorrupted.Includes(result);
		public static bool IsRomHostEntryCorrupted(this nn.Result result) => nn.fs.Rom.ResultRomHostEntryCorrupted.Includes(result);
		public static bool IsRomHostFileDataCorrupted(this nn.Result result) => nn.fs.Rom.ResultRomHostFileDataCorrupted.Includes(result);
		public static bool IsRomHostFileCorrupted(this nn.Result result) => nn.fs.Rom.ResultRomHostFileCorrupted.Includes(result);
		public static bool IsInvalidRomHostHandle(this nn.Result result) => nn.fs.Rom.ResultInvalidRomHostHandle.Includes(result);
		public static bool MaybeHandleErrors(this nn.Result result, string op)
		{
			var ret = result.IsSuccess();
			if (!ret)
				InternalLogException(result, op);
			return ret;
		}

		static partial void InternalLogException(nn.Result result, string op);

#if DEBUG
		static partial void InternalLogException(nn.Result result, string op)
		{
			UnityEngine.Debug.Log($"{op} {result.GetError()}");
		}
#endif

		public static SwitchError GetError(this nn.Result result)
		{
			if (result.IsPathNotFound()) return SwitchError.PathNotFound;
			if (result.IsPathAlreadyExists()) return SwitchError.PathAlreadyExists;
			if (result.IsTargetLocked()) return SwitchError.TargetLocked;
			if (result.IsDirectoryNotEmpty()) return SwitchError.DirectoryNotEmpty;
			if (result.IsDirectoryStatusChanged()) return SwitchError.DirectoryStatusChanged;
			if (result.IsUsableSpaceNotEnough()) return SwitchError.UsableSpaceNotEnough;
			if (result.IsUnsupportedSdkVersion()) return SwitchError.UnsupportedSdkVersion;
			if (result.IsMountNameAlreadyExists()) return SwitchError.MountNameAlreadyExists;
			if (result.IsTargetNotFound()) return SwitchError.TargetNotFound;
			if (result.IsUsableSpaceNotEnoughForSaveData()) return SwitchError.UsableSpaceNotEnoughForSaveData;
			if (result.IsSaveDataHostFileSystemCorrupted()) return SwitchError.SaveDataHostFileSystemCorrupted;
			if (result.IsSaveDataHostEntryCorrupted()) return SwitchError.SaveDataHostEntryCorrupted;
			if (result.IsSaveDataHostFileDataCorrupted()) return SwitchError.SaveDataHostFileDataCorrupted;
			if (result.IsSaveDataHostFileCorrupted()) return SwitchError.SaveDataHostFileCorrupted;
			if (result.IsInvalidSaveDataHostHandle()) return SwitchError.InvalidSaveDataHostHandle;
			if (result.IsHostFileSystemCorrupted()) return SwitchError.HostFileSystemCorrupted;
			if (result.IsHostEntryCorrupted()) return SwitchError.HostEntryCorrupted;
			if (result.IsHostFileDataCorrupted()) return SwitchError.HostFileDataCorrupted;
			if (result.IsHostFileCorrupted()) return SwitchError.HostFileCorrupted;
			if (result.IsInvalidHostHandle()) return SwitchError.InvalidHostHandle;
			if (result.IsRomHostFileSystemCorrupted()) return SwitchError.RomHostFileSystemCorrupted;
			if (result.IsRomHostEntryCorrupted()) return SwitchError.RomHostEntryCorrupted;
			if (result.IsRomHostFileDataCorrupted()) return SwitchError.RomHostFileDataCorrupted;
			if (result.IsRomHostFileCorrupted()) return SwitchError.RomHostFileCorrupted;
			if (result.IsInvalidRomHostHandle()) return SwitchError.InvalidRomHostHandle;
			return SwitchError.None;
		}
	}

	[Flags]
	public enum SwitchError
	{
		None = 0,
		PathNotFound,
		PathAlreadyExists,
		TargetLocked,
		DirectoryNotEmpty,
		DirectoryStatusChanged,
		UsableSpaceNotEnough,
		UnsupportedSdkVersion,
		MountNameAlreadyExists,
		TargetNotFound,
		UsableSpaceNotEnoughForSaveData,
		SaveDataHostFileSystemCorrupted,
		SaveDataHostEntryCorrupted,
		SaveDataHostFileDataCorrupted,
		SaveDataHostFileCorrupted,
		InvalidSaveDataHostHandle,
		HostFileSystemCorrupted,
		HostEntryCorrupted,
		HostFileDataCorrupted,
		HostFileCorrupted,
		InvalidHostHandle,
		RomHostFileSystemCorrupted,
		RomHostEntryCorrupted,
		RomHostFileDataCorrupted,
		RomHostFileCorrupted,
		InvalidRomHostHandle,
	}
}
#endif