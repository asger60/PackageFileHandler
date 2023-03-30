#if UNITY_EDITOR || !UNITY_SWITCH

using System;
using System.IO;
using System.Text;

namespace SpoiledCat.SimpleIO
{
#if SIMPLEIO_INTERNAL
	internal
#else
	public
#endif
	partial class FileSystem : INativeFileSystem
	{
		public string Resolve(string path) => GetCompleteRealPath(path);

		public static bool IsPathRooted(string path) => Path.IsPathRooted(path);


		public void WriteAllLines(string path, string[] contents)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"WriteAllLines requires a rooted path but got {path}", nameof(path));
			File.WriteAllLines(path, contents);
		}

		public string[] ReadAllLines(string path)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"ReadAllLines requires a rooted path but got {path}", nameof(path));
			return File.ReadAllLines(path);
		}
		public void WriteLines(string path, string[] contents)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"WriteLines requires a rooted path but got {path}", nameof(path));

			using var fs = File.AppendText(path);
			foreach (var line in contents)
				fs.WriteLine(line);
		}

		public void WriteAllText(string path, string contents)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"WriteAllText requires a rooted path but got {path}", nameof(path));
			File.WriteAllText(path, contents);
		}

		public void WriteAllText(string path, string contents, Encoding encoding)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"WriteAllText requires a rooted path but got {path}", nameof(path));
			File.WriteAllText(path, contents, encoding);
		}

		public void WriteLine(string path, string line)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"WriteLines requires a rooted path but got {path}", nameof(path));

			using var fs = File.AppendText(path);
			fs.WriteLine(line);
		}

		public string ReadAllText(string path)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"ReadAllText requires a rooted path but got {path}", nameof(path));
			return File.ReadAllText(path);
		}

		public string ReadAllText(string path, Encoding encoding)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"ReadAllText requires a rooted path but got {path}", nameof(path));
			return File.ReadAllText(path, encoding);
		}

		public bool FileExists(string filename)
		{
			if (!IsPathRooted(filename))
				throw new ArgumentException($"FileExists requires a rooted path but got {filename}",
					nameof(filename));
			return File.Exists(filename);
		}

		public bool DirectoryExists(string path)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"DirectoryExists requires a rooted path but got {path}", nameof(path));
			return Directory.Exists(path);
		}

		public byte[] ReadAllBytes(string path)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"ReadAllBytes requires a rooted path but got {path}", nameof(path));
			return File.ReadAllBytes(path);
		}

		public void WriteAllBytes(string path, byte[] bytes)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"WriteAllBytes requires a rooted path but got {path}", nameof(path));
			File.WriteAllBytes(path, bytes);
		}

		public void WriteAllBytes(string path, byte[] bytes, long length)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"WriteAllBytes requires a rooted path but got {path}", nameof(path));

			using var fs = File.Open(path, FileMode.Create);
			if (length <= int.MaxValue)
			{
				fs.Write(bytes, 0, (int)length);
			}
			else
			{
				const int chunkSize = 8192;
				byte[] buffer = new byte[chunkSize];
				long offset = 0;
				while (offset + chunkSize < length)
				{
					Array.Copy(bytes, offset, buffer, 0, chunkSize);
					fs.Write(buffer, 0, chunkSize);
					offset += chunkSize;
				}

				if (offset < length)
				{
					int remaining = (int)(length - offset);
					Array.Copy(bytes, offset, buffer, 0, remaining);
					fs.Write(buffer, 0, remaining);
				}
			}
		}

		public void DirectoryCreate(string path)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"DirectoryCreate requires a rooted path but got {path}", nameof(path));
			Directory.CreateDirectory(path);
		}

		public void FileCopy(string sourceFileName, string destFileName, bool overwrite)
		{
			if (!IsPathRooted(sourceFileName))
				throw new ArgumentException($"FileMove requires a rooted path but got {sourceFileName}", nameof(sourceFileName));
			if (!IsPathRooted(destFileName))
				throw new ArgumentException($"FileMove requires a rooted path but got {destFileName}", nameof(destFileName));
			File.Copy(sourceFileName, destFileName, overwrite);
		}

		public void FileDelete(string path)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"FileDelete requires a rooted path but got {path}", nameof(path));
			File.Delete(path);
		}

		public void DirectoryDelete(string path, bool recursive)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"DirectoryDelete requires a rooted path but got {path}", nameof(path));
			Directory.Delete(path, recursive);
		}

		public void FileMove(string sourceFileName, string destFileName)
		{
			if (!IsPathRooted(sourceFileName))
				throw new ArgumentException($"FileMove requires a rooted path but got {sourceFileName}", nameof(sourceFileName));
			if (!IsPathRooted(destFileName))
				throw new ArgumentException($"FileMove requires a rooted path but got {destFileName}", nameof(destFileName));
			File.Move(sourceFileName, destFileName);
		}

		public void DirectoryMove(string source, string dest)
		{
			if (!IsPathRooted(source))
				throw new ArgumentException($"DirectoryMove requires a rooted path but got {source}", nameof(source));
			if (!IsPathRooted(dest))
				throw new ArgumentException($"DirectoryMove requires a rooted path but got {dest}", nameof(dest));
			Directory.Move(source, dest);
		}

		public Stream OpenRead(string path)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"OpenRead requires a rooted path but got {path}", nameof(path));
			return File.OpenRead(path);
		}

		public Stream OpenWrite(string path, FileMode mode)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"OpenWrite requires a rooted path but got {path}", nameof(path));
			return new FileStream(path, mode);
		}

		public bool IsWindows
		{
			get
			{
				if (isWindows.HasValue)
					return isWindows.Value;
				return Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;
			}
			set => isWindows = value;
		}

		public bool IsLinux
		{
			get
			{
				if (isLinux.HasValue)
					return isLinux.Value;
				return Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/proc");
			}
			set => isLinux = value;
		}

		public bool IsMac
		{
			get
			{
				if (isMac.HasValue)
					return isMac.Value;
				return Environment.OSVersion.Platform == PlatformID.MacOSX ||
				       (Environment.OSVersion.Platform == PlatformID.Unix && !Directory.Exists("/proc"));
			}
			set => isMac = value;
		}
	}
}

#endif