using Engine;

namespace Game;

public static class ClipboardManager
{
	public static string ClipboardString
	{
		get
		{
			return Window.ClipboardString;
		}
		set
		{
			Window.ClipboardString = value;
		}
	}
}
