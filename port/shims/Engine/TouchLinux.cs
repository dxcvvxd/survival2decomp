using System;
using System.Collections.Generic;

namespace Engine.Input;

/// <summary>
/// Linux Touch: desktop has no touch hardware and GLFW exposes none, so no touch
/// locations are ever reported. The public API is identical to the original.
/// (Deliberately NOT synthesized from the mouse: the game treats any touch input as
/// "controlled by touch", which hides the crosshair and pops up the mobile move/look
/// pads. Mouse input is delivered through Mouse only.)
/// </summary>
public static class Touch
{
	public static ReadOnlyList<TouchLocation> TouchLocations => ReadOnlyList<TouchLocation>.Empty;

	public static event Action<TouchLocation> TouchPressed;

	public static event Action<TouchLocation> TouchReleased;

	public static event Action<TouchLocation> TouchMoved;

	internal static void Initialize()
	{
	}

	internal static void Dispose()
	{
	}

	public static void Clear()
	{
	}

	internal static void BeforeFrame()
	{
	}

	internal static void AfterFrame()
	{
	}
}
