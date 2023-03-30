//-----------------------------------------------------------------------
// The MIT License(MIT)
// =====================
//
// Copyright © `2017-2022` `Andreia Gaita`
// Copyright © `2015-2017` `Lucas Meijer`
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// </license>
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.opensource.org/licenses/mit-license.php
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SpoiledCat.SimpleIO
{
#if SIMPLEIO_INTERNAL
	internal
#else
	public
#endif
	partial class FileSystem : INativeFileSystem
	{
		private static Func<string, string> getCompleteRealPathFunc = null;
		private string currentDirectory;
		private string homeDirectory;
		private bool? isLinux;
		private bool? isMac;
		private bool? isWindows;
		private string appData;
		private string localAppData;
		private string commonAppData;

		public FileSystem()
		{}

		/// <summary>
		/// Initialize the filesystem object with the path passed in set as the current directory
		/// </summary>
		/// <param name="directory">Current directory</param>
		public FileSystem(string directory)
		{
			currentDirectory = directory;
		}

		public string GetFolderPath(Environment.SpecialFolder folder)
		{
			switch (folder)
			{
				case Environment.SpecialFolder.LocalApplicationData:
					if (localAppData == null)
					{
						if (SPath.IsMac)
							localAppData = SPath.HomeDirectory.Combine("Library", "Application Support");
						else
							localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToSPath();
					}
					return localAppData;
				case Environment.SpecialFolder.CommonApplicationData:
					if (commonAppData == null)
					{
						if (SPath.IsWindows)
							commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).ToSPath();
						else
						{
							// there is no such thing on the mac that is guaranteed to be user accessible (/usr/local might not be)
							commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToSPath();
						}
					}
					return commonAppData;
				case Environment.SpecialFolder.ApplicationData:
					if (appData == null)
					{
						if (SPath.IsMac)
							appData = SPath.HomeDirectory.Combine("Library", "Preferences");
						else
							appData= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToSPath();
					}
					return appData;

				default:
					return "";
			}
		}

		public IEnumerable<string> GetDirectories(string path)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"GetDirectories requires a rooted path but got {path}", nameof(path));
			return Directory.GetDirectories(path);
		}
		public IEnumerable<string> GetDirectories(string path, string pattern)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"GetDirectories requires a rooted path but got {path}", nameof(path));
			return Directory.GetDirectories(path, pattern);
		}

		public IEnumerable<string> GetDirectories(string path, string pattern, SearchOption searchOption)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"GetDirectories requires a rooted path but got {path}", nameof(path));
			return Directory.GetDirectories(path, pattern, searchOption);
		}

		public IEnumerable<string> GetFiles(string path) => GetFiles(path, "*");

		public IEnumerable<string> GetFiles(string path, string pattern)
		{
			if (!IsPathRooted(path))
				throw new ArgumentException($"GetFiles requires a rooted path but got {path}", nameof(path));
			return Directory.GetFiles(path, pattern);
		}

		public IEnumerable<string> GetFiles(string path, string pattern, SearchOption searchOption)
		{
			foreach (var file in GetFiles(path, pattern))
				yield return file;
			if (searchOption != SearchOption.AllDirectories)
				yield break;
			if (SPath.IsUnix)
			{
				try
				{
					path = Resolve(path);
				}
				catch
				{}
			}

			foreach (var dir in GetDirectories(path))
			{
				var realdir = dir;
				if (SPath.IsUnix)
				{
					try
					{
						realdir = Resolve(dir);
					}
					catch
					{}
				}
				if (path != realdir)
				{
					foreach (var file in GetFiles(dir, pattern, searchOption))
						yield return file;
				}
			}
		}

		public string Combine(string path1, string path2) => Path.Combine(path1, path2);
		public string Combine(string path1, string path2, string path3) => Path.Combine(Path.Combine(path1, path2), path3);
		public string GetFullPath(string path) => Path.GetFullPath(path);
		public string ChangeExtension(string path, string extension) => Path.ChangeExtension(path, extension);
		public string GetFileNameWithoutExtension(string fileName) => Path.GetFileNameWithoutExtension(fileName);

		public bool ExistingPathIsDirectory(string path)
		{
			var attr = File.GetAttributes(path);
			return (attr & FileAttributes.Directory) == FileAttributes.Directory;
		}

		public string GetRandomFileName() => Path.GetRandomFileName();

		public string CurrentDirectory
		{
			get => currentDirectory ?? Directory.GetCurrentDirectory();
			set
			{
				if (!IsPathRooted(value))
					throw new ArgumentException("SetCurrentDirectory requires a rooted path", nameof(value));
				currentDirectory = value;
			}
		}

		public string HomeDirectory
		{
			get
			{
				if (homeDirectory == null)
				{
					if (SPath.IsUnix)
						homeDirectory = new SPath(Environment.GetEnvironmentVariable("HOME"));
					else
						homeDirectory = new SPath(Environment.GetEnvironmentVariable("USERPROFILE"));
				}
				return homeDirectory;
			}
			set => homeDirectory = value;
		}

		public string LocalAppData
		{
			get => localAppData ?? (localAppData = GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
			set => localAppData = value;
		}

		public string CommonAppData
		{
			get => commonAppData ?? (commonAppData = GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			set => commonAppData = value;
		}

		public string AppData
		{
			get => appData ?? (appData = GetFolderPath(Environment.SpecialFolder.ApplicationData));
			set => appData = value;
		}

		public string TempPath => Path.GetTempPath();
		public char DirectorySeparatorChar => Path.DirectorySeparatorChar;

		private static Func<string, string> GetCompleteRealPath
		{
			get
			{
				if (getCompleteRealPathFunc == null)
				{
					var asm = AppDomain.CurrentDomain.GetAssemblies()
									.FirstOrDefault(x => x.FullName.StartsWith("Mono.Posix"));
					if (asm != null)
					{
						var type = asm.GetType("Mono.Unity.UnixPath");
						if (type != null)
						{
							var method = type.GetMethod("GetCompleteRealPath",
								BindingFlags.Static | BindingFlags.Public);
							if (method != null)
							{
								getCompleteRealPathFunc = (p) => {
									var ret = method.Invoke(null, new object[] { p.ToString() });
									if (ret != null)
										return ret.ToString();
									return p;
								};
							}
						}
					}

					if (getCompleteRealPathFunc == null)
						getCompleteRealPathFunc = p => p;
				}
				return getCompleteRealPathFunc;
			}
		}
	}
}
