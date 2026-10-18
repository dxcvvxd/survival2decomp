using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public static class MotdManager
{
	public class Message
	{
		public List<Line> Lines = new List<Line>();
	}

	public class Line
	{
		public float Time;

		public XElement Node;

		public string Text;
	}

	private static string m_url = "http://files.kaalus.com:30096/motd/Motd-2.4.zip";

	private static Message m_message;

	public static Message MessageOfTheDay
	{
		get
		{
			return m_message;
		}
		set
		{
			m_message = value;
			if (MessageOfTheDayUpdated != null)
			{
				MessageOfTheDayUpdated();
			}
		}
	}

	public static event Action MessageOfTheDayUpdated;

	public static void ForceRedownload()
	{
		SettingsManager.MotdNextUpdateTime = DateTime.MinValue;
	}

	public static void Initialize()
	{
		if (VersionsManager.Version != VersionsManager.LastLaunchedVersion)
		{
			ForceRedownload();
		}
	}

	public static void Update()
	{
		if (Time.PeriodicEvent(2.0, 0.0))
		{
			DateTime now = DateTime.Now;
			if (now >= SettingsManager.MotdNextUpdateTime)
			{
				SettingsManager.MotdNextUpdateTime = now.AddHours(8.0);
				Log.Information("Downloading MOTD");
				AnalyticsManager.LogEvent("[MotdManager] Downloading MOTD", new AnalyticsParameter("Time", DateTime.Now.ToString("HH:mm:ss.fff")));
				WebManager.Get(m_url, null, null, null, delegate(byte[] result)
				{
					try
					{
						string motdLastDownloadedData = UnpackMotd(result);
						MessageOfTheDay = null;
						SettingsManager.MotdLastDownloadedData = motdLastDownloadedData;
						Log.Information("Downloaded MOTD");
						AnalyticsManager.LogEvent("[MotdManager] Downloaded MOTD", new AnalyticsParameter("Time", DateTime.Now.ToString("HH:mm:ss.fff")), new AnalyticsParameter("Url", m_url));
					}
					catch (Exception ex)
					{
						Log.Error("Failed processing MOTD string. Reason: " + ex.Message);
					}
				}, delegate(Exception error)
				{
					Log.Error("Failed downloading MOTD. Reason: {0}", error.Message);
				});
			}
		}
		if (MessageOfTheDay == null && !string.IsNullOrEmpty(SettingsManager.MotdLastDownloadedData))
		{
			MessageOfTheDay = ParseMotd(SettingsManager.MotdLastDownloadedData);
			if (MessageOfTheDay == null)
			{
				SettingsManager.MotdLastDownloadedData = string.Empty;
			}
		}
	}

	private static string UnpackMotd(byte[] data)
	{
		string text = "motd.xml";
		using (MemoryStream stream = new MemoryStream(data))
		{
			using ZipArchive zipArchive = ZipArchive.Open(stream);
			foreach (ZipArchiveEntry item in zipArchive.ReadCentralDir())
			{
				if (item.FilenameInZip.ToLower() == text)
				{
					MemoryStream memoryStream = new MemoryStream();
					zipArchive.ExtractFile(item, memoryStream);
					memoryStream.Position = 0L;
					using StreamReader streamReader = new StreamReader(memoryStream);
					return streamReader.ReadToEnd();
				}
			}
		}
		throw new InvalidOperationException($"\"{text}\" file not found in Motd zip archive.");
	}

	private static Message ParseMotd(string dataString)
	{
		try
		{
			int num = dataString.IndexOf("<Motd");
			if (num >= 0)
			{
				int num2 = dataString.IndexOf("</Motd>");
				if (num2 >= 0 && num2 > num)
				{
					num2 += 7;
				}
				XElement xElement = XmlUtils.LoadXmlFromString(dataString.Substring(num, num2 - num), throwOnError: true);
				Message message = new Message();
				foreach (XElement item2 in xElement.Elements())
				{
					if (Widget.IsNodeIncludedOnCurrentPlatform(item2))
					{
						Line item = new Line
						{
							Time = XmlUtils.GetAttributeValue<float>(item2, "Time"),
							Node = item2.Elements().FirstOrDefault(),
							Text = item2.Value
						};
						message.Lines.Add(item);
					}
				}
				return message;
			}
			throw new InvalidOperationException("Invalid MOTD data string.");
		}
		catch (Exception ex)
		{
			Log.Warning("Failed extracting MOTD string. Reason: " + ex.Message);
		}
		return null;
	}
}
