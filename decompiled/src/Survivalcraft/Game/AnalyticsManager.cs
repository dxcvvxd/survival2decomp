using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Engine.Graphics;

namespace Game;

public static class AnalyticsManager
{
	private static double LastSendTime = double.MinValue;

	public static string AnalyticsVersion => string.Empty;

	public static void Initialize()
	{
	}

	public static void LogError(string message, Exception error)
	{
		try
		{
			double realTime = Time.RealTime;
			if (realTime - LastSendTime < 15.0)
			{
				return;
			}
			LastSendTime = realTime;
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary.Add("Platform", VersionsManager.Platform.ToString());
			dictionary.Add("BuildConfiguration", VersionsManager.BuildConfiguration.ToString());
			dictionary.Add("DeviceModel", DeviceManager.DeviceModel);
			dictionary.Add("OSVersion", DeviceManager.OperatingSystemVersion);
			dictionary.Add("Is64bit", (Marshal.SizeOf<nint>() == 8).ToString());
			dictionary.Add("DisplayDevice", Display.DeviceDescription);
			dictionary.Add("FreeSpace", Storage.FreeSpace / 1024 / 1024 + "MB");
			dictionary.Add("TotalAvailableMemory", Utilities.GetTotalAvailableMemory() / 1024 + "kB");
			dictionary.Add("RealTime", Time.RealTime.ToString("0.000") + "s");
			dictionary.Add("WindowSize", Window.IsCreated ? Window.Size.ToString() : "Window not created");
			dictionary.Add("FullErrorMessage", ExceptionManager.MakeFullErrorMessage(message, error));
			dictionary.Add("ExceptionType", error.GetType().ToString());
			dictionary.Add("ExceptionStackTrace", (error.StackTrace != null) ? AbbreviateStackTrace(error.StackTrace) : "No stack trace in exception");
			MemoryStream memoryStream = new MemoryStream();
			DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true);
			BinaryWriter binaryWriter = new BinaryWriter(deflateStream);
			binaryWriter.Write(3735928559u);
			binaryWriter.Write((byte)dictionary.Count);
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				binaryWriter.Write(item.Key);
				binaryWriter.Write(item.Value);
			}
			deflateStream.Dispose();
			memoryStream.Position = 0L;
			global::_003C_003Ey__InlineArray4<object> buffer = default(global::_003C_003Ey__InlineArray4<object>);
			buffer[0] = 1;
			buffer[1] = "Survivalcraft";
			buffer[2] = VersionsManager.Version;
			buffer[3] = "Error";
			Post("quality.kaalus.com", 30099, string.Format("/{0}/{1}/{2}/{3}", (ReadOnlySpan<object?>)buffer), memoryStream.ToArray());
		}
		catch (Exception)
		{
		}
	}

	public static void LogEvent(string eventName, params AnalyticsParameter[] parameters)
	{
	}

	private static string AbbreviateStackTrace(string stackTrace)
	{
		stackTrace = stackTrace.Replace("System.Collections.Generic.", "");
		stackTrace = stackTrace.Replace("System.Collections.", "");
		stackTrace = stackTrace.Replace("System.IO.", "");
		stackTrace = stackTrace.Replace(" System.", " ");
		stackTrace = stackTrace.Replace("Engine.Audio.", "");
		stackTrace = stackTrace.Replace("Engine.Input.", "");
		stackTrace = stackTrace.Replace("Engine.Graphics.", "");
		stackTrace = stackTrace.Replace("Engine.Media.", "");
		stackTrace = stackTrace.Replace("Engine.Content.", "");
		stackTrace = stackTrace.Replace("Engine.Serialization.", "");
		stackTrace = stackTrace.Replace(" Engine.", " ");
		stackTrace = stackTrace.Replace("C:\\Sources\\Engine\\Engine\\", "");
		stackTrace = stackTrace.Replace("C:\\Sources\\Survivalcraft\\Game\\", "");
		stackTrace = stackTrace.Replace(":line ", ":");
		return stackTrace;
	}

	private static void Post(string host, int port, string path, byte[] data)
	{
		Task.Run(delegate
		{
			try
			{
				string s = $"POST {path} HTTP/1.1\r\nHost: {host}\r\nContent-Type: application/octet-stream\r\nContent-Length: {data.Length}\r\n\r\n";
				byte[] bytes = Encoding.UTF8.GetBytes(s);
				byte[] array = new byte[bytes.Length + data.Length];
				Array.Copy(bytes, 0, array, 0, bytes.Length);
				Array.Copy(data, 0, array, bytes.Length, data.Length);
				using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.Connect(host, port);
				socket.Send(array);
			}
			catch (Exception)
			{
			}
		});
	}
}
