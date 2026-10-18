using Engine;

namespace Game;

public static class ScreenResolutionManager
{
	public static float ApproximateScreenDpi { get; private set; }

	public static float ApproximateScreenInches => MathUtils.Sqrt(Window.ScreenSize.X * Window.ScreenSize.X + Window.ScreenSize.Y * Window.ScreenSize.Y) / ApproximateScreenDpi;

	static ScreenResolutionManager()
	{
		// 96 DPI at typical desktop resolutions yields UIScale = 1 (see SettingsManager).
		ApproximateScreenDpi = 96f;
		ApproximateScreenDpi = MathUtils.Clamp(ApproximateScreenDpi, 96f, 800f);
	}
}
