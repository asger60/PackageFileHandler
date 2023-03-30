#if UNITY_SWITCH && !UNITY_EDITOR

//-----------------------------------------------------------------------
// Copyright © `2017-2022` `Andreia Gaita`
// All rights reserved
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;

namespace SpoiledCat.SimpleIO
{
	partial class FileSystem : INativeFileSystem
	{

		public bool IsWindows { get => false; set {;}}
		public bool IsLinux { get => true; set {;}}
		public bool IsMac { get => false; set {;}}

		public string Resolve(string path) => path;
		
		public static bool IsPathRooted(string path)
		{
			return path.IndexOf(":/", StringComparison.InvariantCulture) > 1;
		}

		public void WriteAllLines(string path, string[] contents)
		{
			var data = Encoding.UTF8.GetBytes(string.Join("\n", contents));
			WriteAllBytes(path, data);
		}

		public void WriteAllText(string path, string contents) => WriteAllText(path, contents, Encoding.UTF8);

		public void WriteAllText(string path, string contents, Encoding encoding)
		{
			var data = encoding.GetBytes(contents);
			WriteAllBytes(path, data);
		}

		public void WriteLines(string path, string[] contents)
		{
			var data = Encoding.UTF8.GetBytes(string.Join("\n", contents));
			WriteAllBytes(path, data, data.LongLength, true);
		}

		public void WriteLine(string path, string line)
		{
			var data = Encoding.UTF8.GetBytes(line);
			WriteAllBytes(path, data, data.LongLength, true);
		}

		public string[] ReadAllLines(string path)
		{
			var contents = ReadAllText(path);
			return contents?.Split('\n') ?? Array.Empty<string>();
		}

		public string ReadAllText(string path) => ReadAllText(path, Encoding.UTF8);

		public string ReadAllText(string path, Encoding encoding)
		{
			var data = ReadAllBytes(path);
			return encoding.GetString(data);
		}

		public Stream OpenRead(string path)
		{
			var data = ReadAllBytes(path);
			return new MemoryStream(data);
		}

		public bool FileExists(string path)
		{
			nn.fs.EntryType entryType = 0;
			nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, path);
			return result.IsSuccess() && entryType == nn.fs.EntryType.File;
		}

		public void FileDelete(string path)
		{
			nn.fs.File.Delete(path).MaybeHandleErrors("File.Delete");
		}

		public void FileCopy(string sourceFileName, string destFileName, bool overwrite)
		{
			var source = ReadAllBytes(sourceFileName);
			WriteAllBytes(destFileName, source);
		}

		public void FileMove(string sourceFileName, string destFileName)
		{
			var source = ReadAllBytes(sourceFileName);
			WriteAllBytes(destFileName, source);
			FileDelete(sourceFileName);
		}

		public bool DirectoryExists(string path)
		{
			nn.fs.EntryType entryType = 0;
			nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, path);
			return result.IsSuccess() && entryType == nn.fs.EntryType.Directory;
		}

		public void DirectoryCreate(string path)
		{
			var result = nn.fs.Directory.Create(path);
			result.abortUnlessSuccess();
		}

		public void DirectoryMove(string source, string dest)
		{
			throw new NotImplementedException();
		}

		public void DirectoryDelete(string path, bool recursive)
		{
			if (recursive)
				nn.fs.Directory.DeleteRecursively(path).MaybeHandleErrors("Directory.DeleteRecursively");
			else
				nn.fs.Directory.Delete(path).MaybeHandleErrors("Directory.Delete");
		}

		public Stream OpenWrite(string path, FileMode mode)
		{
			throw new NotImplementedException();
		}

		public byte[] ReadAllBytes(string path)
		{
			nn.fs.FileHandle handle = default;
			if (nn.fs.File.Open(ref handle, path, nn.fs.OpenFileMode.Read).IsPathNotFound()) {
				return Array.Empty<byte>();
			}

	        long fileSize = 0;
		    if (!nn.fs.File.GetSize(ref fileSize, handle).MaybeHandleErrors("File.GetSize"))
				return Array.Empty<byte>();

			byte[] data = new byte[fileSize];
			if (!nn.fs.File.Read(handle, 0, data, fileSize).MaybeHandleErrors("File.Read"))
				return Array.Empty<byte>();

			nn.fs.File.Close(handle);
			return data;
		}

		public void WriteAllBytes(string path, byte[] bytes) => WriteAllBytes(path, bytes, bytes.LongLength, false);
		public void WriteAllBytes(string path, byte[] bytes, long length) => WriteAllBytes(path, bytes, length, false);

		private void WriteAllBytes(string path, byte[] bytes, long length, bool append)
		{
			nn.Result result = default;

			nn.fs.FileHandle handle = default;
			var flags = nn.fs.OpenFileMode.Write | nn.fs.OpenFileMode.AllowAppend;

			result = nn.fs.File.Open(ref handle, path, flags);
			long offset = 0;

			if (!result.IsSuccess())
			{
				if (result.IsPathNotFound())
				{
					if (!nn.fs.File.Create(path, length).MaybeHandleErrors("File.Create"))
						return;

					if (!nn.fs.File.Open(ref handle, path, flags).MaybeHandleErrors("File.Open"))
						return;
				}
				else
				{
					result.MaybeHandleErrors("File.Open");
					return;
				}
			}
			else
			{
				long fileSize = 0;
				result = nn.fs.File.GetSize(ref fileSize, handle);
				if (!append)
				{
					if (!result.IsSuccess() || fileSize != length)
					{
						nn.fs.File.SetSize(handle, length).MaybeHandleErrors("File.SetSize");
					}
				}
				else
				{
					nn.fs.File.SetSize(handle, fileSize + length).MaybeHandleErrors("File.SetSize");
					offset = fileSize;
				}
			}

			if (!nn.fs.File.Write(handle, offset, bytes, length, nn.fs.WriteOption.Flush).MaybeHandleErrors("File.Write"))
				return;

			nn.fs.File.Close(handle);

			var mountName = path.Substring(0, path.IndexOf(":", StringComparison.InvariantCulture));
			nn.fs.FileSystem.Commit(mountName).MaybeHandleErrors("FileSystem.Commit");
		}
	}
}

#endif