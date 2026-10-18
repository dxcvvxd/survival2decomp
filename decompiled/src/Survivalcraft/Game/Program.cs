using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Engine;
using Engine.Graphics;

namespace Game;

public static class Program
{
	private static double m_frameBeginTime;

	private static double m_cpuEndTime;

	private static List<Uri> m_urisToHandle = new List<Uri>();

	public static float LastFrameTime { get; private set; }

	public static float LastCpuFrameTime { get; private set; }

	public static event Action<Uri> HandleUri;

	[STAThread]
	public static void Main()
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
		Log.RemoveAllLogSinks();
		Log.AddLogSink(new GameLogSink());
		Window.HandleUri += HandleUriHandler;
		Window.Deactivated += DeactivatedHandler;
		Window.Frame += FrameHandler;
		Window.UnhandledException += delegate(UnhandledExceptionInfo e)
		{
			ExceptionManager.ReportExceptionToUser("Unhandled exception.", e.Exception);
			e.IsHandled = true;
		};
		Window.Run(0, 0, Engine.WindowMode.Resizable, "Survivalcraft 2");
	}

	private static void HandleUriHandler(Uri uri)
	{
		m_urisToHandle.Add(uri);
	}

	private static void DeactivatedHandler()
	{
		GC.Collect();
	}

	private static void FrameHandler()
	{
		if (Time.FrameIndex < 0)
		{
			Display.Clear(Vector4.Zero, 1f);
		}
		else if (Time.FrameIndex == 0)
		{
			Initialize();
		}
		else
		{
			Run();
		}
	}

	private static void Initialize()
	{
		_003C_003Ey__InlineArray15<object> buffer = default(_003C_003Ey__InlineArray15<object>);
		buffer[0] = DateTime.Now;
		buffer[1] = VersionsManager.Version;
		buffer[2] = VersionsManager.BuildConfiguration;
		buffer[3] = VersionsManager.Platform;
		buffer[4] = DeviceManager.DeviceModel;
		buffer[5] = DeviceManager.OperatingSystemVersion;
		buffer[6] = Storage.FreeSpace / 1024 / 1024;
		buffer[7] = ScreenResolutionManager.ApproximateScreenDpi;
		buffer[8] = ScreenResolutionManager.ApproximateScreenInches;
		buffer[9] = Window.Size;
		buffer[10] = Environment.ProcessorCount;
		buffer[11] = Utilities.GetTotalAvailableMemory() / 1024 / 1024;
		buffer[12] = Marshal.SizeOf<nint>() == 8;
		buffer[13] = RuntimeInformation.ProcessArchitecture;
		buffer[14] = RuntimeInformation.OSArchitecture;
		Log.Information(string.Format("Survivalcraft starting up at {0}, Version={1}, BuildConfiguration={2}, Platform={3}, DeviceModel={4}, OSVersion={5}, Storage.AvailableFreeSpace={6}MB, ApproximateScreenDpi={7:0.0}, ApproxScreenInches={8:0.0}, ScreenResolution={9}, ProcessorsCount={10}, RAM={11}MB, 64bit={12}, ProcessArchitecture={13} OSArchitecture={14}", (ReadOnlySpan<object?>)buffer));
		MarketplaceManager.Initialize();
		SettingsManager.Initialize();
		AnalyticsManager.Initialize();
		VersionsManager.Initialize();
		ExternalContentManager.Initialize();
		ContentManager.Initialize();
		ScreensManager.Initialize();
		MusicManager.Initialize();
	}

	private static void Run()
	{
		VrManager.WaitGetPoses();
		double realTime = Time.RealTime;
		LastFrameTime = (float)(realTime - m_frameBeginTime);
		LastCpuFrameTime = (float)(m_cpuEndTime - m_frameBeginTime);
		m_frameBeginTime = realTime;
		Window.PresentationInterval = ((!VrManager.IsVrStarted) ? SettingsManager.PresentationInterval : 0);
		try
		{
			if (ExceptionManager.Error == null)
			{
				while (m_urisToHandle.Count > 0)
				{
					Uri obj = m_urisToHandle[0];
					m_urisToHandle.RemoveAt(0);
					HandleUri?.Invoke(obj);
				}
				PerformanceManager.Update();
				MotdManager.Update();
				MusicManager.Update();
				ScreensManager.Update();
				DialogsManager.Update();
			}
			else
			{
				ExceptionManager.UpdateExceptionScreen();
			}
		}
		catch (Exception e)
		{
			ExceptionManager.ReportExceptionToUser(null, e);
			ScreensManager.SwitchScreen("MainMenu");
		}
		try
		{
			Display.RenderTarget = null;
			if (ExceptionManager.Error == null)
			{
				ScreensManager.Draw();
				PerformanceManager.Draw();
				ScreenCaptureManager.Run();
			}
			else
			{
				ExceptionManager.DrawExceptionScreen();
			}
			m_cpuEndTime = Time.RealTime;
		}
		catch (Exception e2)
		{
			ExceptionManager.ReportExceptionToUser(null, e2);
			ScreensManager.SwitchScreen("MainMenu");
		}
	}
}
