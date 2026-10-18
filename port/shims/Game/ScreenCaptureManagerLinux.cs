using System;

namespace Game;

public static class ScreenCaptureManager
{
	public static void Run()
	{
		// screenshot/media-store integration is Android-only; capture comes later
	}

	public static void CapturePhoto(Action success, Action<Exception> failure)
	{
		failure?.Invoke(new NotSupportedException("Photo capture is not available on desktop yet."));
	}
}
