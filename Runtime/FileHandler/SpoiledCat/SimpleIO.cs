#if UNITY_SWITCH && !UNITY_EDITOR
#define TT_UNITY_SWITCH
#endif

//-----------------------------------------------------------------------
// <license file="SimpleIO.cs">
//
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

#if UNITY_5_3_OR_NEWER
using SerializedAttr=UnityEngine.SerializeField;
#else
using SerializedAttr=System.NonSerializedAttribute;
#endif
namespace SpoiledCat.SimpleIO
{
	[Serializable]
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
#if SIMPLEIO_INTERNAL
	internal
#else
	public
#endif
		struct SPath : IEquatable<SPath>, IComparable
	{
		public static SPath Default;

		[SerializedAttr]
		private string[] elements;
		[SerializedAttr]
		private string driveLetter;
		[SerializedAttr]
		private bool isInitialized;
		[SerializedAttr]
		private bool isRelative;

#region construction

		public SPath(string path)
		{
			EnsureNotNull(path, "path");

			isInitialized = true;

			path = ParseDriveLetter(path, out driveLetter);

			if (path == "/")
			{
				isRelative = false;
				elements = new string[] {};
			}
			else
			{
				var split = path.Split('/', '\\');

				isRelative = driveLetter == null && IsRelativeFromSplitString(split);

				elements = ParseSplitStringIntoElements(split.Where(s => s.Length > 0).ToArray(), isRelative);
			}
		}

		public static (SPath, bool) TryParse(string path)
		{
			if (path == null) return (SPath.Default, false);
			var p = new SPath(path);
			return (p, !p.IsEmpty || p.IsRoot);
		}

		private SPath(string[] elements, bool isRelative, string driveLetter)
		{
			this.elements = elements;
			this.isRelative = isRelative;
			this.driveLetter = driveLetter;
			this.isInitialized = true;
		}

		private static string[] ParseSplitStringIntoElements(IEnumerable<string> inputs, bool isRelative)
		{
			var stack = new List<string>();

			foreach (var input in inputs.Where(input => input.Length != 0))
			{
				if (input == ".")
				{
					if ((stack.Count > 0) && (stack.Last() != "."))
						continue;
				}
				else if (input == "..")
				{
					if (HasNonDotDotLastElement(stack))
					{
						stack.RemoveAt(stack.Count - 1);
						continue;
					}
					if (!isRelative)
						throw new ArgumentException("You cannot create a path that tries to .. past the root");
				}
				stack.Add(input);
			}
			return stack.ToArray();
		}

		private static bool HasNonDotDotLastElement(List<string> stack)
		{
			return stack.Count > 0 && stack[stack.Count - 1] != "..";
		}

		private static string ParseDriveLetter(string path, out string driveLetter)
		{
			var idx = path.IndexOf(':');
#if TT_UNITY_SWITCH
			if (idx >= 2 && path.Length > idx + 1 && path[idx + 1] == '/' || path[idx + 1] == '\\')
#else

			if (path.Length >= 3 && path[1] == ':' && (path[2] == '/' || path[2] == '\\'))
#endif
			{
				driveLetter = path.Substring(0, idx);
				return path.Substring(idx + 1);
			}

			driveLetter = null;
			return path;
		}

		private static bool IsRelativeFromSplitString(string[] split)
		{
			if (split.Length < 2)
				return true;

			return split[0].Length != 0 || !split.Any(s => s.Length > 0);
		}

		public SPath Combine(params string[] append)
		{
			return Combine(append.Select(a => new SPath(a)).ToArray());
		}

		public SPath Combine(params SPath[] append)
		{
			ThrowIfNotInitialized();

			if (!append.All(p => p.IsRelative))
				throw new ArgumentException("You cannot .Combine a non-relative path");

			return new SPath(
				ParseSplitStringIntoElements(elements.Concat(append.SelectMany(p => p.elements)), IsRelative),
				IsRelative, DriveLetter);
		}

		public SPath Parent {
			get {
				ThrowIfNotInitialized();

				if (elements.Length == 0)
					throw new InvalidOperationException("Parent is called on an empty path");

				var newElements = elements.Take(elements.Length - 1).ToArray();

				return new SPath(newElements, IsRelative, DriveLetter);
			}
		}

		public SPath RelativeTo(SPath path)
		{
			ThrowIfNotInitialized();

			if (!IsChildOf(path))
			{
				if (!IsRelative && !path.IsRelative && DriveLetter != path.DriveLetter)
					throw new ArgumentException(
						"Path.RelativeTo() was invoked with two paths that are on different volumes. invoked on: " +
						ToString() + " asked to be made relative to: " + path);

				SPath commonParent = Default;
				foreach (var parent in RecursiveParents)
				{
					commonParent = path.RecursiveParents.FirstOrDefault(otherParent => otherParent == parent);

					if (commonParent.IsInitialized)
						break;
				}

				if (!commonParent.IsInitialized)
					throw new ArgumentException("Path.RelativeTo() was unable to find a common parent between " +
					                            ToString() + " and " + path);

				if (IsRelative && path.IsRelative && commonParent.IsEmpty)
					throw new ArgumentException(
						"Path.RelativeTo() was invoked with two relative paths that do not share a common parent.  Invoked on: " +
						ToString() + " asked to be made relative to: " + path);

				var depthDiff = path.Depth - commonParent.Depth;
				return new SPath(
					Enumerable.Repeat("..", depthDiff).Concat(elements.Skip(commonParent.Depth)).ToArray(), true,
					null);
			}

			return new SPath(elements.Skip(path.elements.Length).ToArray(), true, null);
		}

		public SPath GetCommonParent(SPath path)
		{
			ThrowIfNotInitialized();

			if (!IsChildOf(path))
			{
				if (!IsRelative && !path.IsRelative && DriveLetter != path.DriveLetter)
					return Default;

				SPath commonParent = Default;
				foreach (var parent in new List<SPath> { this }.Concat(RecursiveParents))
				{
					commonParent = path.RecursiveParents.FirstOrDefault(otherParent => otherParent == parent);
					if (commonParent.IsInitialized)
						break;
				}

				if (IsRelative && path.IsRelative && (!commonParent.IsInitialized || commonParent.IsEmpty))
					return Default;
				return commonParent;
			}
			return path;
		}

		public SPath ChangeExtension(string extension)
		{
			ThrowIfNotInitialized();
			ThrowIfRoot();

			var newElements = (string[])elements.Clone();
			newElements[newElements.Length - 1] =
				FileSystem.ChangeExtension(elements[elements.Length - 1], WithDot(extension));
			if (string.IsNullOrEmpty(extension))
				newElements[newElements.Length - 1] = newElements[newElements.Length - 1].TrimEnd('.');
			return new SPath(newElements, IsRelative, DriveLetter);
		}

#endregion construction

#region inspection

		public bool IsRelative => isRelative;
		public bool IsInitialized => isInitialized;

		public string FileName {
			get {
				ThrowIfNotInitialized();
				ThrowIfRoot();

				return elements.Last();
			}
		}

		public string FileNameWithoutExtension {
			get {
				ThrowIfNotInitialized();

				return FileSystem.GetFileNameWithoutExtension(FileName);
			}
		}

		public IEnumerable<string> Elements {
			get {
				ThrowIfNotInitialized();
				return elements;
			}
		}

		public int Depth {
			get {
				ThrowIfNotInitialized();
				return elements.Length;
			}
		}


		public bool Exists()
		{
			ThrowIfNotInitialized();
			return FileExists() || DirectoryExists();
		}

		public bool Exists(string append)
		{
			ThrowIfNotInitialized();
			if (String.IsNullOrEmpty(append))
			{
				return Exists();
			}
			return Exists(new SPath(append));
		}

		public bool Exists(SPath append)
		{
			ThrowIfNotInitialized();
			if (!append.IsInitialized)
				return Exists();
			return FileExists(append) || DirectoryExists(append);
		}

		public bool DirectoryExists()
		{
			ThrowIfNotInitialized();
			return FileSystem.DirectoryExists(MakeAbsolute());
		}

		public bool DirectoryExists(string append)
		{
			ThrowIfNotInitialized();
			if (String.IsNullOrEmpty(append))
				return DirectoryExists();
			return DirectoryExists(new SPath(append));
		}

		public bool DirectoryExists(SPath append)
		{
			ThrowIfNotInitialized();
			if (!append.IsInitialized)
				return DirectoryExists();
			return FileSystem.DirectoryExists(Combine(append).MakeAbsolute());
		}

		public bool FileExists()
		{
			ThrowIfNotInitialized();
			return FileSystem.FileExists(MakeAbsolute());
		}

		public bool FileExists(string append)
		{
			ThrowIfNotInitialized();
			if (String.IsNullOrEmpty(append))
				return FileExists();
			return FileExists(new SPath(append));
		}

		public bool FileExists(SPath append)
		{
			ThrowIfNotInitialized();
			if (!append.IsInitialized)
				return FileExists();
			return FileSystem.FileExists(Combine(append).MakeAbsolute());
		}

		public string ExtensionWithDot {
			get {
				ThrowIfNotInitialized();
				if (IsRoot)
					throw new ArgumentException("A root directory does not have an extension");

				var last = elements.Last();
				var index = last.LastIndexOf(".");
				if (index < 0) return String.Empty;
				return last.Substring(index);
			}
		}

		public string InQuotes()
		{
			return "\"" + ToString() + "\"";
		}

		public string InQuotes(SlashMode slashMode)
		{
			return "\"" + ToString(slashMode) + "\"";
		}

		public override string ToString()
		{
			return ToString(SlashMode.Native);
		}

		public string ToString(SlashMode slashMode)
		{
			if (!IsInitialized)
				return String.Empty;

			// Check if it's linux root /
			if (IsRoot && DriveLetter == null)
				return Slash(slashMode).ToString();

			if (IsRelative && elements.Length == 0)
				return ".";

			var sb = new StringBuilder();
			if (DriveLetter != null)
			{
				sb.Append(DriveLetter);
				sb.Append(":");
			}
			if (!IsRelative)
				sb.Append(Slash(slashMode));
			var first = true;
			foreach (var element in elements)
			{
				if (!first)
					sb.Append(Slash(slashMode));

				sb.Append(element);
				first = false;
			}
			return sb.ToString();
		}

		public static implicit operator string(SPath path)
		{
			return path.ToString();
		}

		static char Slash(SlashMode slashMode)
		{
			switch (slashMode)
			{
				case SlashMode.Backward:
					return '\\';
				case SlashMode.Forward:
					return '/';
				default:
					return FileSystem.DirectorySeparatorChar;
			}
		}

		public override bool Equals(object other)
		{
			if (other is SPath path)
			{
				return Equals(path);
			}
			return false;
		}

		public bool Equals(SPath p)
		{
			if (p.IsInitialized != IsInitialized)
				return false;

			// return early if we're comparing two NPath.Default instances
			if (!IsInitialized)
				return true;

			if (p.IsRelative != IsRelative)
				return false;

			if (!string.Equals(p.DriveLetter, DriveLetter, PathStringComparison))
				return false;

			if (p.elements.Length != elements.Length)
				return false;

			for (var i = 0; i != elements.Length; i++)
				if (!string.Equals(p.elements[i], elements[i], PathStringComparison))
					return false;

			return true;
		}

		public static bool operator ==(SPath lhs, SPath rhs)
		{
			return lhs.Equals(rhs);
		}

		const int prime = 16777619;
		const uint primeOffset = 2166136261;
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = (int)primeOffset;
				hash *= prime + IsInitialized.GetHashCode();
				if (!IsInitialized)
					return hash;
				hash *= prime + IsRelative.GetHashCode();
				foreach (var element in elements)
					hash *= prime + (IsUnix ? element : element?.ToUpperInvariant())?.GetHashCode() ?? 0;
				hash *= prime + (IsUnix ? DriveLetter : DriveLetter?.ToUpperInvariant())?.GetHashCode() ?? 0;
				return hash;
			}
		}

		public int CompareTo(object other)
		{
			if (!(other is SPath path))
				return -1;

			return string.Compare(ToString(), path.ToString(), PathStringComparison);
		}

		public static bool operator !=(SPath lhs, SPath rhs)
		{
			return !(lhs.Equals(rhs));
		}

		public bool HasExtension(params string[] extensions)
		{
			ThrowIfNotInitialized();
			var extensionWithDotLower = ExtensionWithDot.ToLower();
			return extensions.Any(e => WithDot(e).ToLower() == extensionWithDotLower);
		}

		private static string WithDot(string extension)
		{
			return extension.StartsWith(".") ? extension : "." + extension;
		}

		public bool IsEmpty {
			get {
				ThrowIfNotInitialized();
				return elements.Length == 0;
			}
		}

		public bool IsRoot {
			get {
				return IsEmpty && !IsRelative;
			}
		}

#endregion inspection

#region directory enumeration

		public IEnumerable<SPath> Files(string filter, bool recurse = false)
		{
			return FileSystem
				.GetFiles(MakeAbsolute(), filter,
					recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select(s => new SPath(s));
		}

		public IEnumerable<SPath> Files(bool recurse = false)
		{
			return Files("*", recurse);
		}

		public IEnumerable<SPath> Contents(string filter, bool recurse = false)
		{
			return Files(filter, recurse).Concat(Directories(filter, recurse));
		}

		public IEnumerable<SPath> Contents(bool recurse = false)
		{
			return Contents("*", recurse);
		}

		public IEnumerable<SPath> Directories(string filter, bool recurse = false)
		{
			return FileSystem.GetDirectories(MakeAbsolute(), filter,
				recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select(s => new SPath(s));
		}

		public IEnumerable<SPath> Directories(bool recurse = false)
		{
			return Directories("*", recurse);
		}

#endregion

#region filesystem writing operations

		public SPath CreateFile()
		{
			ThrowIfNotInitialized();
			ThrowIfRoot();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllBytes(MakeAbsolute(), Array.Empty<byte>());
			return this;
		}

		public SPath CreateFile(string file)
		{
			return CreateFile(new SPath(file));
		}

		public SPath CreateFile(SPath file)
		{
			ThrowIfNotInitialized();
			if (!file.IsRelative)
				throw new ArgumentException(
					"You cannot call CreateFile() on an existing path with a non relative argument");
			return Combine(file).CreateFile();
		}

		public SPath CreateDirectory()
		{
			ThrowIfNotInitialized();

			if (IsRoot)
				throw new NotSupportedException(
					"CreateDirectory is not supported on a root level directory because it would be dangerous:" +
					ToString());

			FileSystem.DirectoryCreate(MakeAbsolute());
			return this;
		}

		public SPath CreateDirectory(string directory)
		{
			return CreateDirectory(new SPath(directory));
		}

		public SPath CreateDirectory(SPath directory)
		{
			ThrowIfNotInitialized();
			if (!directory.IsRelative)
				throw new ArgumentException("Cannot call CreateDirectory with an absolute argument");

			return Combine(directory).CreateDirectory();
		}

		public SPath Copy(string dest)
		{
			return Copy(new SPath(dest));
		}

		public SPath Copy(string dest, Func<SPath, bool> fileFilter)
		{
			return Copy(new SPath(dest), fileFilter);
		}

		public SPath Copy(SPath dest)
		{
			return Copy(dest, p => true);
		}

		public SPath Copy(SPath dest, Func<SPath, bool> fileFilter)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(dest);

			if (dest.IsRelative)
				dest = Parent.Combine(dest);

			if (dest.DirectoryExists())
				return CopyWithDeterminedDestination(dest.Combine(FileName), fileFilter);

			return CopyWithDeterminedDestination(dest, fileFilter);
		}

		public SPath MakeAbsolute()
		{
			ThrowIfNotInitialized();

			if (!IsRelative)
				return this;

			return SPath.CurrentDirectory.Combine(this);
		}

		public SPath MakeRelative()
		{
			ThrowIfNotInitialized();
			if (IsRelative)
				return this;
			var sb = new StringBuilder();
			var first = true;
			foreach (var element in elements)
			{
				if (!first)
					sb.Append('/');
				first = false;
				sb.Append(element);
			}
			return new SPath(sb.ToString());
		}
		SPath CopyWithDeterminedDestination(SPath absoluteDestination, Func<SPath, bool> fileFilter)
		{
			if (absoluteDestination.IsRelative)
				throw new ArgumentException("absoluteDestination must be absolute");

			if (FileExists())
			{
				if (!fileFilter(absoluteDestination))
					return Default;

				absoluteDestination.EnsureParentDirectoryExists();

				FileSystem.FileCopy(MakeAbsolute(), absoluteDestination.MakeAbsolute(), true);
				return absoluteDestination;
			}

			if (DirectoryExists())
			{
				absoluteDestination.EnsureDirectoryExists();
				foreach (var thing in Contents())
					thing.CopyWithDeterminedDestination(absoluteDestination.Combine(thing.RelativeTo(this)),
						fileFilter);
				return absoluteDestination;
			}

			throw new ArgumentException("Copy() called on path that doesnt exist: " + ToString());
		}

		public void Delete(DeleteMode deleteMode = DeleteMode.Normal)
		{
			ThrowIfNotInitialized();

			if (IsRoot)
				throw new NotSupportedException(
					"Delete is not supported on a root level directory because it would be dangerous:" + ToString());

			var isFile = FileExists();
			var isDir = DirectoryExists();
			if (!isFile && !isDir)
				throw new InvalidOperationException("Trying to delete a path that does not exist: " + ToString());

			try
			{
				if (isFile)
				{
					FileSystem.FileDelete(MakeAbsolute());
				}
				else
				{
					FileSystem.DirectoryDelete(MakeAbsolute(), true);
				}
			}
			catch (IOException)
			{
				if (deleteMode == DeleteMode.Normal)
					throw;
			}
		}

		public void DeleteIfExists(DeleteMode deleteMode = DeleteMode.Normal)
		{
			ThrowIfNotInitialized();

			if (FileExists() || DirectoryExists())
				Delete(deleteMode);
		}

		public SPath DeleteContents()
		{
			ThrowIfNotInitialized();

			if (IsRoot)
				throw new NotSupportedException(
					"DeleteContents is not supported on a root level directory because it would be dangerous:" +
					ToString());

			if (FileExists())
				throw new InvalidOperationException("It is not valid to perform this operation on a file");

			if (DirectoryExists())
			{
				try
				{
					Files().Delete();
					Directories().Delete();
				}
				catch (IOException)
				{
					if (Files(true).Any())
						throw;
				}

				return this;
			}

			return EnsureDirectoryExists();
		}

		public static SPath CreateTempDirectory(string myprefix)
		{
			var random = new Random();
			while (true)
			{
				var candidate = new SPath(FileSystem.TempPath+ "/" + myprefix + "_" + random.Next());
				if (!candidate.Exists())
					return candidate.CreateDirectory();
			}
		}

		public static SPath GetTempFilename(string myprefix = "")
		{
			var random = new Random();
			var prefix = FileSystem.TempPath+ "/" + (String.IsNullOrEmpty(myprefix) ? "" : myprefix + "_");
			while (true)
			{
				var candidate = new SPath(prefix + random.Next());
				if (!candidate.Exists())
					return candidate;
			}
		}

		public SPath Move(string dest)
		{
			return Move(new SPath(dest));
		}

		public SPath Move(SPath dest)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(dest);

			if (IsRoot)
				throw new NotSupportedException(
					"Move is not supported on a root level directory because it would be dangerous:" + ToString());

			if (IsRelative)
				return MakeAbsolute().Move(dest);

			if (dest.IsRelative)
				return Move(Parent.Combine(dest));

			if (dest.DirectoryExists())
				return Move(dest.Combine(FileName));

			if (FileExists())
			{
				dest.DeleteIfExists();
				dest.EnsureParentDirectoryExists();
				FileSystem.FileMove(MakeAbsolute(), dest.MakeAbsolute());
				return dest;
			}

			if (DirectoryExists())
			{
				FileSystem.DirectoryMove(MakeAbsolute(), dest.MakeAbsolute());
				return dest;
			}

			throw new ArgumentException(
				"Move() called on a path that doesn't exist: " + MakeAbsolute().ToString());
		}

		public SPath WriteAllText(string contents)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllText(MakeAbsolute(), contents);
			return this;
		}

		public string ReadAllText()
		{
			ThrowIfNotInitialized();
			return FileSystem.ReadAllText(MakeAbsolute());
		}

		public SPath WriteAllText(string contents, Encoding encoding)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllText(MakeAbsolute(), contents, encoding);
			return this;
		}

		public string ReadAllText(Encoding encoding)
		{
			ThrowIfNotInitialized();
			return FileSystem.ReadAllText(MakeAbsolute(), encoding);
		}

		public SPath WriteLines(string[] contents)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteLines(MakeAbsolute(), contents);
			return this;
		}

		public SPath WriteLine(string line)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteLine(MakeAbsolute(), line);
			return this;
		}

		public SPath WriteAllLines(string[] contents)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllLines(MakeAbsolute(), contents);
			return this;
		}

		public string[] ReadAllLines()
		{
			ThrowIfNotInitialized();
			return FileSystem.ReadAllLines(MakeAbsolute());
		}

		public SPath WriteAllBytes(byte[] contents)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllBytes(MakeAbsolute(), contents);
			return this;
		}

		public SPath WriteAllBytes(byte[] contents, long length)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllBytes(MakeAbsolute(), contents, length);
			return this;
		}

		public byte[] ReadAllBytes()
		{
			ThrowIfNotInitialized();
			return FileSystem.ReadAllBytes(MakeAbsolute());
		}

		public Stream OpenRead()
		{
			ThrowIfNotInitialized();
			return FileSystem.OpenRead(MakeAbsolute());
		}

		public Stream OpenWrite(FileMode mode)
		{
			ThrowIfNotInitialized();
			return FileSystem.OpenWrite(MakeAbsolute(), mode);
		}
		public IEnumerable<SPath> CopyFiles(SPath destination, bool recurse, Func<SPath, bool> fileFilter = null)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(destination);

			destination.EnsureDirectoryExists();
			var _this = this;
			return Files(recurse)
				   .Where(fileFilter ?? AlwaysTrue)
				   .Select(file => file.Copy(destination.Combine(file.RelativeTo(_this))))
				   .ToArray();
		}

		public IEnumerable<SPath> MoveFiles(SPath destination, bool recurse, Func<SPath, bool> fileFilter = null)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(destination);

			if (IsRoot)
				throw new NotSupportedException(
					"MoveFiles is not supported on this directory because it would be dangerous:" + ToString());

			destination.EnsureDirectoryExists();
			var _this = this;
			return Files(recurse)
			   .Where(fileFilter ?? AlwaysTrue)
			   .Select(file => file.Move(destination.Combine(file.RelativeTo(_this))))
			   .ToArray();
		}

		public void DeleteFiles(bool recurse, Func<SPath, bool> fileFilter = null)
		{
			ThrowIfNotInitialized();

			if (IsRoot)
				throw new NotSupportedException(
					"DeleteFiles is not supported on this directory because it would be dangerous:" + ToString());

			foreach (var file in Files(recurse).Where(fileFilter ?? AlwaysTrue))
			{
				file.DeleteIfExists();
			}
		}

#endregion

#region special paths

		private static SPath currentDirectory;
		public static SPath CurrentDirectory {
			get {
				if (!currentDirectory.IsInitialized)
					currentDirectory = new SPath(FileSystem.CurrentDirectory);
				return currentDirectory;
			}
		}

		private static SPath homeDirectory;

		public static SPath HomeDirectory {
			get {
				if (!homeDirectory.IsInitialized)
					homeDirectory = new SPath(FileSystem.HomeDirectory);
				return homeDirectory;
			}
		}
		private static SPath localAppData;
		public static SPath LocalAppData {
			get {
				if (!localAppData.IsInitialized)
					localAppData = new SPath(FileSystem.LocalAppData);
				return localAppData;
			}
		}

		private static SPath commonAppData;
		public static SPath CommonAppData {
			get {
				if (!commonAppData.IsInitialized)
					commonAppData = new SPath(FileSystem.CommonAppData);
				return commonAppData;
			}
		}

		private static SPath appData;
		public static SPath AppData {
			get {
				if (!appData.IsInitialized)
					appData = new SPath(FileSystem.AppData);
				return appData;
			}
		}


		private static SPath systemTemp;
		public static SPath SystemTemp {
			get {
				if (!systemTemp.IsInitialized)
					systemTemp = new SPath(FileSystem.TempPath);
				return systemTemp;
			}
		}

#endregion

		private void ThrowIfRelative()
		{
			if (IsRelative)
				throw new ArgumentException(
					"You are attempting an operation on a Path that requires an absolute path, but the path is relative");
		}

		private void ThrowIfRoot()
		{
			if (IsRoot)
				throw new ArgumentException(
					"You are attempting an operation that is not valid on a root level directory");
		}

		private void ThrowIfNotInitialized()
		{
			if (!IsInitialized)
				throw new InvalidOperationException("You are attemping an operation on an null path");
		}

		private static void ThrowIfNotInitialized(SPath path)
		{
			path.ThrowIfNotInitialized();
		}

		public SPath EnsureDirectoryExists(string append = "")
		{
			ThrowIfNotInitialized();

			if (string.IsNullOrEmpty(append))
			{
				if (IsRoot)
					return this;
				if (DirectoryExists())
					return this;
				EnsureParentDirectoryExists();
				CreateDirectory();
				return this;
			}
			return EnsureDirectoryExists(new SPath(append));
		}

		public SPath EnsureDirectoryExists(SPath append)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(append);

			var combined = Combine(append);
			if (combined.DirectoryExists())
				return combined;
			combined.EnsureParentDirectoryExists();
			combined.CreateDirectory();
			return combined;
		}

		public SPath EnsureParentDirectoryExists()
		{
			ThrowIfNotInitialized();

			var parent = Parent;
			parent.EnsureDirectoryExists();
			return this;
		}

		public SPath FileMustExist()
		{
			ThrowIfNotInitialized();

			if (!FileExists())
				throw new FileNotFoundException("File was expected to exist : " + ToString());

			return this;
		}

		public SPath DirectoryMustExist()
		{
			ThrowIfNotInitialized();

			if (!DirectoryExists())
				throw new DirectoryNotFoundException("Expected directory to exist : " + ToString());

			return this;
		}

		public bool IsChildOf(string potentialBasePath)
		{
			return IsChildOf(new SPath(potentialBasePath));
		}

		public bool IsChildOf(SPath potentialBasePath)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(potentialBasePath);

			if ((IsRelative && !potentialBasePath.IsRelative) || !IsRelative && potentialBasePath.IsRelative)
				throw new ArgumentException("You can only call IsChildOf with two relative paths, or with two absolute paths");

			// If the other path is the root directory, then anything is a child of it as long as it's not a Windows path
			if (potentialBasePath.IsRoot)
			{
				if (DriveLetter != potentialBasePath.DriveLetter)
					return false;
				return true;
			}

			if (IsEmpty)
				return false;

			if (Equals(potentialBasePath))
				return true;

			return Parent.IsChildOf(potentialBasePath);
		}

		public IEnumerable<SPath> RecursiveParents {
			get {
				ThrowIfNotInitialized();
				var candidate = this;
				while (true)
				{
					if (candidate.IsEmpty)
						yield break;

					candidate = candidate.Parent;
					yield return candidate;
				}
			}
		}

		public SPath ParentContaining(string needle)
		{
			return ParentContaining(new SPath(needle));
		}

		public SPath ParentContaining(SPath needle)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(needle);
			ThrowIfRelative();

			return RecursiveParents.FirstOrDefault(p => p.Exists(needle));
		}

		static bool AlwaysTrue(SPath p)
		{
			return true;
		}

		private static INativeFileSystem fileSystem;
		public static INativeFileSystem FileSystem {
			get {
				if (fileSystem == null)
#if UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
#if UNITY_EDITOR
					fileSystem = new FileSystem(Directory.GetCurrentDirectory());
#else
					fileSystem = new FileSystem(UnityEngine.Application.dataPath);
#endif
#else
					fileSystem = new FileSystem(Directory.GetCurrentDirectory());
#endif
				return fileSystem;
			}
			set { fileSystem = value; }
		}

		public static bool IsUnix => FileSystem.IsLinux || FileSystem.IsMac;
		public static bool IsWindows => FileSystem.IsWindows;
		public static bool IsLinux => FileSystem.IsLinux;
		public static bool IsMac => FileSystem.IsMac;

		private static StringComparison? pathStringComparison;
		private static StringComparison PathStringComparison {
			get {
				// this is lazily evaluated because IsUnix uses the FileSystem object and that can be set
				// after static constructors happen here
				if (!pathStringComparison.HasValue)
					pathStringComparison = IsUnix ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
				return pathStringComparison.Value;
			}
		}

		private static T EnsureNotNull<T>(T value, string name,
#if !NET_35 && UNITY_EDITOR
[System.Runtime.CompilerServices.CallerMemberName]
#endif
			string caller = "")
		{
			if (value != null) return value;
			string message = $"In {caller}, '{name}' must not be null";
			throw new ArgumentNullException(name, message);
		}
		internal string DebuggerDisplay => ToString();

		private string DriveLetter
		{
			get
			{
				if (driveLetter?.Length == 0)
					driveLetter = null;
				return driveLetter;
			}
			set
			{
				if (value?.Length == 0)
					value = null;
				driveLetter = value;
			}
		}
	}

#if SIMPLEIO_INTERNAL
	internal
#else
	public
#endif
		static class Extensions
	{
		public static IEnumerable<SPath> Copy(this IEnumerable<SPath> self, string dest)
		{
			return Copy(self, new SPath(dest));
		}

		public static IEnumerable<SPath> Copy(this IEnumerable<SPath> self, SPath dest)
		{
			if (dest.IsRelative)
				throw new ArgumentException("When copying multiple files, the destination cannot be a relative path");
			dest.EnsureDirectoryExists();
			return self.Select(p => p.Copy(dest.Combine(p.FileName))).ToArray();
		}

		public static IEnumerable<SPath> Move(this IEnumerable<SPath> self, string dest)
		{
			return Move(self, new SPath(dest));
		}

		public static IEnumerable<SPath> Move(this IEnumerable<SPath> self, SPath dest)
		{
			if (dest.IsRelative)
				throw new ArgumentException("When moving multiple files, the destination cannot be a relative path");
			dest.EnsureDirectoryExists();
			return self.Select(p => p.Move(dest.Combine(p.FileName))).ToArray();
		}

		public static IEnumerable<SPath> Delete(this IEnumerable<SPath> self)
		{
			foreach (var p in self)
				p.Delete();
			return self;
		}

		public static IEnumerable<string> InQuotes(this IEnumerable<SPath> self, SlashMode forward = SlashMode.Native)
		{
			return self.Select(p => p.InQuotes(forward));
		}

		public static SPath ToSPath(this string path)
		{
			if (path == null)
				return SPath.Default;
			return new SPath(path);
		}

		public static SPath Resolve(this SPath path)
		{
			if (!path.IsInitialized || !path.Exists())
				return path;
			string fullPath = SPath.FileSystem.GetFullPath(path.ToString());
			if (!SPath.IsUnix)
				return fullPath.ToSPath();
			return SPath.FileSystem.Resolve(fullPath).ToSPath();
		}
		public static SPath CreateTempDirectory(this SPath baseDir, string myprefix = "")
		{
			var random = new Random();
			while (true)
			{
				var candidate = baseDir.Combine(myprefix + "_" + random.Next());
				if (!candidate.Exists())
					return candidate.CreateDirectory();
			}
		}

	}

#if SIMPLEIO_INTERNAL
	internal
#else
	public
#endif
		enum SlashMode
	{
		Native,
		Forward,
		Backward
	}

#if SIMPLEIO_INTERNAL
	internal
#else
	public
#endif
		enum DeleteMode
	{
		Normal,
		Soft
	}

#if SIMPLEIO_INTERNAL
	internal
#else
	public
#endif
	interface INativeFileSystem
	{
		string ChangeExtension(string path, string extension);
		string Combine(string path1, string path2);
		string Combine(string path1, string path2, string path3);
		void DirectoryCreate(string path);
		void DirectoryDelete(string path, bool recursive);
		bool DirectoryExists(string path);
		void DirectoryMove(string toString, string s);
		bool ExistingPathIsDirectory(string path);
		void FileCopy(string sourceFileName, string destFileName, bool overwrite);
		void FileDelete(string path);
		bool FileExists(string path);
		void FileMove(string sourceFileName, string s);
		IEnumerable<string> GetDirectories(string path);
		IEnumerable<string> GetDirectories(string path, string pattern);
		IEnumerable<string> GetDirectories(string path, string pattern, SearchOption searchOption);
		string GetFileNameWithoutExtension(string fileName);
		IEnumerable<string> GetFiles(string path);
		IEnumerable<string> GetFiles(string path, string pattern);
		IEnumerable<string> GetFiles(string path, string pattern, SearchOption searchOption);
		string GetFullPath(string path);
		string GetRandomFileName();
		string GetFolderPath(Environment.SpecialFolder folder);
		Stream OpenRead(string path);
		Stream OpenWrite(string path, FileMode mode);
		byte[] ReadAllBytes(string path);
		string[] ReadAllLines(string path);
		string ReadAllText(string path);
		string ReadAllText(string path, Encoding encoding);
		void WriteAllBytes(string path, byte[] bytes);
		void WriteAllBytes(string path, byte[] bytes, long length);
		void WriteAllLines(string path, string[] contents);
		void WriteAllText(string path, string contents);
		void WriteAllText(string path, string contents, Encoding encoding);
		void WriteLines(string path, string[] contents);
		void WriteLine(string path, string line);

		string Resolve(string path);
		char DirectorySeparatorChar { get; }
		string TempPath { get; }
		string CurrentDirectory { get; set; }
		string HomeDirectory { get; set; }
		string LocalAppData { get; set; }
		string CommonAppData { get; set; }
		string AppData { get; set; }
		bool IsWindows { get; set; }
		bool IsLinux { get; set; }
		bool IsMac { get; set; }
	}
}