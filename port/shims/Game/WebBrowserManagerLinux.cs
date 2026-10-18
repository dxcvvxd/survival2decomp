using System;
using System.Diagnostics;
using Engine;

namespace Game;

public static class WebBrowserManager
{
	public static void LaunchBrowser(string url)
	{
		AnalyticsManager.LogEvent("[WebBrowserManager] Launching browser", new AnalyticsParameter("Url", url));
		if (!url.Contains("://"))
		{
			url = "http://" + url;
		}
		try
		{
			Process.Start(new ProcessStartInfo("xdg-open", "\"" + url.Replace("\"", "\\\"") + "\"")
			{
				UseShellExecute = false
			});
		}
		catch (Exception ex)
		{
			Log.Error($"Error launching web browser with URL \"{url}\". Reason: {ex.Message}");
		}
	}
}
