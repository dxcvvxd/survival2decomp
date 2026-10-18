using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public static class VersionsManager
{
	private static List<VersionConverter> m_versionConverters;

	public static Platform Platform => Platform.Android;

	public static BuildConfiguration BuildConfiguration => BuildConfiguration.Release;

	public static string Version { get; private set; }

	public static string SerializationVersion { get; private set; }

	public static string LastLaunchedVersion { get; private set; }

	static VersionsManager()
	{
		m_versionConverters = new List<VersionConverter>();
		Assembly assembly = typeof(VersionsManager).GetTypeInfo().Assembly;
		AssemblyName assemblyName = new AssemblyName(assembly.FullName);
		global::_003C_003Ey__InlineArray4<object> buffer = default(global::_003C_003Ey__InlineArray4<object>);
		buffer[0] = assemblyName.Version.Major;
		buffer[1] = assemblyName.Version.Minor;
		buffer[2] = assemblyName.Version.Build;
		buffer[3] = assemblyName.Version.Revision;
		Version = string.Format("{0}.{1}.{2}.{3}", (ReadOnlySpan<object?>)buffer);
		SerializationVersion = $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}";
		foreach (TypeInfo definedType in assembly.DefinedTypes)
		{
			if (!definedType.IsAbstract && !definedType.IsInterface && typeof(VersionConverter).GetTypeInfo().IsAssignableFrom(definedType))
			{
				VersionConverter item = (VersionConverter)Activator.CreateInstance(definedType.AsType());
				m_versionConverters.Add(item);
			}
		}
	}

	public static void Initialize()
	{
		LastLaunchedVersion = SettingsManager.LastLaunchedVersion;
		SettingsManager.LastLaunchedVersion = Version;
		if (Version != LastLaunchedVersion)
		{
			AnalyticsManager.LogEvent("[VersionsManager] Upgrade game", new AnalyticsParameter("LastVersion", LastLaunchedVersion), new AnalyticsParameter("CurrentVersion", Version));
		}
	}

	public static void UpgradeProjectXml(XElement projectNode)
	{
		string attributeValue = XmlUtils.GetAttributeValue(projectNode, "Version", "1.0");
		if (!(attributeValue != SerializationVersion))
		{
			return;
		}
		foreach (VersionConverter item in FindTransform(attributeValue, SerializationVersion, m_versionConverters, 0) ?? throw new InvalidOperationException($"Cannot find conversion path from version \"{attributeValue}\" to version \"{SerializationVersion}\""))
		{
			Log.Information($"Upgrading world version \"{item.SourceVersion}\" to \"{item.TargetVersion}\".");
			item.ConvertProjectXml(projectNode);
		}
		string attributeValue2 = XmlUtils.GetAttributeValue(projectNode, "Version", "1.0");
		if (attributeValue2 != SerializationVersion)
		{
			throw new InvalidOperationException($"Upgrade produced invalid project version. Expected \"{SerializationVersion}\", found \"{attributeValue2}\".");
		}
	}

	public static void UpgradeWorld(string directoryName)
	{
		WorldInfo worldInfo = WorldsManager.GetWorldInfo(directoryName);
		if (worldInfo == null)
		{
			throw new InvalidOperationException($"Cannot determine version of world at \"{directoryName}\"");
		}
		if (!(worldInfo.SerializationVersion != SerializationVersion))
		{
			return;
		}
		ProgressManager.UpdateProgress($"Upgrading World To {SerializationVersion}", 0f);
		foreach (VersionConverter item in FindTransform(worldInfo.SerializationVersion, SerializationVersion, m_versionConverters, 0) ?? throw new InvalidOperationException($"Cannot find conversion path from version \"{worldInfo.SerializationVersion}\" to version \"{SerializationVersion}\""))
		{
			Log.Information($"Upgrading world version \"{item.SourceVersion}\" to \"{item.TargetVersion}\".");
			item.ConvertWorld(directoryName);
		}
		WorldInfo worldInfo2 = WorldsManager.GetWorldInfo(directoryName);
		if (worldInfo2.SerializationVersion != SerializationVersion)
		{
			throw new InvalidOperationException($"Upgrade produced invalid project version. Expected \"{SerializationVersion}\", found \"{worldInfo2.SerializationVersion}\".");
		}
		AnalyticsManager.LogEvent("[VersionConverter] Upgrade world", new AnalyticsParameter("SourceVersion", worldInfo.SerializationVersion), new AnalyticsParameter("TargetVersion", SerializationVersion));
	}

	public static int CompareVersions(string v1, string v2)
	{
		string[] array = v1.Split('.');
		string[] array2 = v2.Split('.');
		for (int i = 0; i < MathUtils.Min(array.Length, array2.Length); i++)
		{
			int num = ((!int.TryParse(array[i], out var result) || !int.TryParse(array2[i], out var result2)) ? string.CompareOrdinal(array[i], array2[i]) : (result - result2));
			if (num != 0)
			{
				return num;
			}
		}
		return array.Length - array2.Length;
	}

	private static List<VersionConverter> FindTransform(string sourceVersion, string targetVersion, IEnumerable<VersionConverter> converters, int depth)
	{
		if (depth > 100)
		{
			throw new InvalidOperationException("Too deep recursion when searching for version converters. Check for possible loops in transforms.");
		}
		if (sourceVersion == targetVersion)
		{
			return new List<VersionConverter>();
		}
		List<VersionConverter> result = null;
		int num = int.MaxValue;
		foreach (VersionConverter converter in converters)
		{
			if (converter.SourceVersion == sourceVersion)
			{
				List<VersionConverter> list = FindTransform(converter.TargetVersion, targetVersion, converters, depth + 1);
				if (list != null && list.Count < num)
				{
					num = list.Count;
					list.Insert(0, converter);
					result = list;
				}
			}
		}
		return result;
	}
}
