using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine;

/// <summary>
/// Linux Storage: same path semantics as the Android original
/// ("data:" resolves under the app data directory, "app:" resources are not available).
/// "data:" root: $SURVIVALCRAFT2_HOME if set, else ~/.config/survivalcraft2
/// (settings, logs, worlds, texture/furniture packs, downloaded content).
/// Game items left in the old location (SpecialFolder.Personal) are migrated once.
/// </summary>
public static class Storage
{
	/// <summary>Legacy "data:" root used by earlier builds (user profile / Documents).</summary>
	private static readonly string m_legacyDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

	/// <summary>Current "data:" root: env override or XDG config home.</summary>
	private static readonly string m_dataRoot = ComputeDataRoot();

	private static readonly string[] m_migratableDirectories = new string[6]
	{
		"Worlds", "TexturePacks", "FurniturePacks", "CharacterSkins", "Logs", ".config"
	};

	private static readonly string[] m_migratableFiles = new string[4]
	{
		"Settings.xml", "UserId.dat", "CommunityContentCache.xml", "WorldUpload.tmp"
	};

	private static bool m_migrated;

	private static string ComputeDataRoot()
	{
		string env = Environment.GetEnvironmentVariable("SURVIVALCRAFT2_HOME");
		if (!string.IsNullOrWhiteSpace(env))
		{
			return env.TrimEnd('/');
		}
		string configHome = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		if (string.IsNullOrEmpty(configHome))
		{
			configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
		}
		return Path.Combine(configHome, "survivalcraft2");
	}

	/// <summary>
	/// One-time, item-by-item move of game data from the legacy root into the app
	/// config directory. Never overwrites existing destinations; ignores anything else.
	/// </summary>
	private static void MigrateLegacyData()
	{
		try
		{
			if (string.IsNullOrEmpty(m_legacyDataRoot) || Path.GetFullPath(m_legacyDataRoot) == Path.GetFullPath(m_dataRoot))
			{
				return;
			}
			foreach (string name in m_migratableDirectories)
			{
				string source = Path.Combine(m_legacyDataRoot, name);
				string destination = Path.Combine(m_dataRoot, name);
				if (Directory.Exists(source) && !Directory.Exists(destination) && !File.Exists(destination))
				{
					Directory.CreateDirectory(m_dataRoot);
					Directory.Move(source, destination);
					Log.Debug("Migrated \"{0}\" -> \"{1}\".", source, destination);
				}
			}
			foreach (string name in m_migratableFiles)
			{
				string source = Path.Combine(m_legacyDataRoot, name);
				string destination = Path.Combine(m_dataRoot, name);
				if (File.Exists(source) && !File.Exists(destination) && !Directory.Exists(destination))
				{
					Directory.CreateDirectory(m_dataRoot);
					File.Move(source, destination);
					Log.Debug("Migrated \"{0}\" -> \"{1}\".", source, destination);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("Could not migrate legacy game data: {0}", ex.Message);
		}
	}

	public static long FreeSpace
	{
		get
		{
			try
			{
				string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
				if (string.IsNullOrEmpty(root))
				{
					root = "/";
				}
				DriveInfo driveInfo = new DriveInfo(root);
				return driveInfo.AvailableFreeSpace;
			}
			catch (Exception)
			{
				return long.MaxValue;
			}
		}
	}

	public static bool FileExists(string path)
	{
		return File.Exists(ProcessPath(path, writeAccess: false, failIfApp: true));
	}

	public static bool DirectoryExists(string path)
	{
		return Directory.Exists(ProcessPath(path, writeAccess: false, failIfApp: true));
	}

	public static long GetFileSize(string path)
	{
		return new FileInfo(ProcessPath(path, writeAccess: false, failIfApp: true)).Length;
	}

	public static DateTime GetFileLastWriteTime(string path)
	{
		return File.GetLastWriteTimeUtc(ProcessPath(path, writeAccess: false, failIfApp: true));
	}

	public static Stream OpenFile(string path, OpenFileMode openFileMode)
	{
		if (openFileMode != OpenFileMode.Read && openFileMode != OpenFileMode.ReadWrite && openFileMode != OpenFileMode.Create && openFileMode != OpenFileMode.CreateOrOpen)
		{
			throw new ArgumentException("openFileMode");
		}
		string text = ProcessPath(path, openFileMode != OpenFileMode.Read, failIfApp: false, out var isApp);
		if (isApp)
		{
			throw new InvalidOperationException($"\"app:\" resources are not available on desktop (path \"{path}\").");
		}
		FileMode mode = openFileMode switch
		{
			OpenFileMode.Create => FileMode.Create,
			OpenFileMode.CreateOrOpen => FileMode.OpenOrCreate,
			_ => FileMode.Open,
		};
		FileAccess access = ((openFileMode == OpenFileMode.Read) ? FileAccess.Read : FileAccess.ReadWrite);
		return File.Open(text, mode, access, FileShare.Read);
	}

	public static void DeleteFile(string path)
	{
		File.Delete(ProcessPath(path, writeAccess: true, failIfApp: true));
	}

	public static void CopyFile(string sourcePath, string destinationPath)
	{
		using Stream stream = OpenFile(sourcePath, OpenFileMode.Read);
		using Stream destination = OpenFile(destinationPath, OpenFileMode.Create);
		stream.CopyTo(destination);
	}

	public static void MoveFile(string sourcePath, string destinationPath)
	{
		string sourceFileName = ProcessPath(sourcePath, writeAccess: true, failIfApp: true);
		string text = ProcessPath(destinationPath, writeAccess: true, failIfApp: true);
		File.Delete(text);
		File.Move(sourceFileName, text);
	}

	public static void CreateDirectory(string path)
	{
		Directory.CreateDirectory(ProcessPath(path, writeAccess: true, failIfApp: true));
	}

	public static void DeleteDirectory(string path)
	{
		Directory.Delete(ProcessPath(path, writeAccess: true, failIfApp: true));
	}

	public static void DeleteDirectoryRecursive(string path)
	{
		foreach (string item in ListDirectoryNames(path))
		{
			DeleteDirectoryRecursive(CombinePaths(path, item));
		}
		foreach (string item2 in ListFileNames(path))
		{
			DeleteFile(CombinePaths(path, item2));
		}
		DeleteDirectory(path);
	}

	public static void MoveDirectory(string sourcePath, string destinationPath)
	{
		string text = ProcessPath(sourcePath, writeAccess: true, failIfApp: true);
		string text2 = ProcessPath(destinationPath, writeAccess: true, failIfApp: true);
		if (Path.GetDirectoryName(text) != Path.GetDirectoryName(text2))
		{
			throw new IOException("Moving directories not supported, only renaming.");
		}
		Directory.Move(text, text2);
	}

	public static IEnumerable<string> ListFileNames(string path)
	{
		return from s in Directory.EnumerateFiles(ProcessPath(path, writeAccess: false, failIfApp: true))
			select Path.GetFileName(s);
	}

	public static IEnumerable<string> ListDirectoryNames(string path)
	{
		return from s in Directory.EnumerateDirectories(ProcessPath(path, writeAccess: false, failIfApp: true))
			select Path.GetFileName(s) into s
			where s != ".__override__"
			select s;
	}

	public static string ReadAllText(string path)
	{
		return ReadAllText(path, Encoding.UTF8);
	}

	public static string ReadAllText(string path, Encoding encoding)
	{
		using StreamReader streamReader = new StreamReader(OpenFile(path, OpenFileMode.Read), encoding);
		return streamReader.ReadToEnd();
	}

	public static void WriteAllText(string path, string text)
	{
		WriteAllText(path, text, Encoding.UTF8);
	}

	public static void WriteAllText(string path, string text, Encoding encoding)
	{
		using StreamWriter streamWriter = new StreamWriter(OpenFile(path, OpenFileMode.Create), encoding);
		streamWriter.Write(text);
	}

	public static byte[] ReadAllBytes(string path)
	{
		using BinaryReader binaryReader = new BinaryReader(OpenFile(path, OpenFileMode.Read));
		return binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
	}

	public static void WriteAllBytes(string path, byte[] bytes)
	{
		using BinaryWriter binaryWriter = new BinaryWriter(OpenFile(path, OpenFileMode.Create));
		binaryWriter.Write(bytes);
	}

	public static string GetSystemPath(string path)
	{
		return ProcessPath(path, writeAccess: false, failIfApp: true);
	}

	public static string GetExtension(string path)
	{
		int num = path.LastIndexOf('.');
		if (num >= 0)
		{
			return path.Substring(num);
		}
		return string.Empty;
	}

	public static string GetFileName(string path)
	{
		int num = path.LastIndexOf('/');
		if (num >= 0)
		{
			return path.Substring(num + 1);
		}
		return path;
	}

	public static string GetFileNameWithoutExtension(string path)
	{
		string fileName = GetFileName(path);
		int num = fileName.LastIndexOf('.');
		if (num >= 0)
		{
			return fileName.Substring(0, num);
		}
		return fileName;
	}

	public static string GetDirectoryName(string path)
	{
		int num = path.LastIndexOf('/');
		if (num >= 0)
		{
			return path.Substring(0, num).TrimEnd('/');
		}
		return string.Empty;
	}

	public static string CombinePaths(params string[] paths)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < paths.Length; i++)
		{
			if (paths[i].Length > 0)
			{
				stringBuilder.Append(paths[i]);
				if (i < paths.Length - 1 && (stringBuilder.Length == 0 || stringBuilder[stringBuilder.Length - 1] != '/'))
				{
					stringBuilder.Append('/');
				}
			}
		}
		return stringBuilder.ToString();
	}

	public static string ChangeExtension(string path, string extension)
	{
		return CombinePaths(GetDirectoryName(path), GetFileNameWithoutExtension(path)) + extension;
	}

	private static string ProcessPath(string path, bool writeAccess, bool failIfApp)
	{
		bool isApp;
		return ProcessPath(path, writeAccess, failIfApp, out isApp);
	}

	private static string ProcessPath(string path, bool writeAccess, bool failIfApp, out bool isApp)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (Path.DirectorySeparatorChar != '/')
		{
			path = path.Replace('/', Path.DirectorySeparatorChar);
		}
		if (Path.DirectorySeparatorChar != '\\')
		{
			path = path.Replace('\\', Path.DirectorySeparatorChar);
		}
		if (path.StartsWith("app:"))
		{
			if (failIfApp)
			{
				throw new InvalidOperationException($"Access denied to \"{path}\".");
			}
			isApp = true;
			return path.Substring(4).TrimStart(Path.DirectorySeparatorChar);
		}
		if (path.StartsWith("data:"))
		{
			isApp = false;
			if (!m_migrated)
			{
				m_migrated = true;
				MigrateLegacyData();
				Directory.CreateDirectory(m_dataRoot);
			}
			return Path.Combine(m_dataRoot, path.Substring(5).TrimStart(Path.DirectorySeparatorChar));
		}
		throw new InvalidOperationException($"Invalid path \"{path}\".");
	}
}
