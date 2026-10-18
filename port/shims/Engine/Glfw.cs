using System;
using System.Runtime.InteropServices;

namespace Engine;

/// <summary>
/// Minimal GLFW 3 bindings for the Linux host: ES2 context window, input callbacks, clipboard.
/// </summary>
internal static class Glfw
{
	private const string Lib = "glfw";

	// client API / hints
	public const int CLIENT_API = 0x00022001;
	public const int OPENGL_ES_API = 0x30002;
	public const int CONTEXT_VERSION_MAJOR = 0x00022002;
	public const int CONTEXT_VERSION_MINOR = 0x00022003;
	public const int RESIZABLE = 0x00020003;
	public const int VISIBLE = 0x00020004;
	public const int FOCUSED = 0x00020001;
	public const int FOCUS_ON_SHOW = 0x0002000C;
	public const int SAMPLES = 0x0002100D;

	// input modes
	public const int CURSOR = 0x00033001;
	public const int CURSOR_NORMAL = 0x00034001;
	public const int CURSOR_HIDDEN = 0x00034002;
	public const int CURSOR_DISABLED = 0x00034003;

	// actions
	public const int RELEASE = 0;
	public const int PRESS = 1;
	public const int REPEAT = 2;

	// mouse buttons
	public const int MOUSE_BUTTON_LEFT = 0;
	public const int MOUSE_BUTTON_RIGHT = 1;
	public const int MOUSE_BUTTON_MIDDLE = 2;

	// standard keys (ASCII + GLFW special range)
	public const int KEY_SPACE = 32;
	public const int KEY_APOSTROPHE = 39;
	public const int KEY_COMMA = 44;
	public const int KEY_MINUS = 45;
	public const int KEY_PERIOD = 46;
	public const int KEY_SLASH = 47;
	public const int KEY_0 = 48;
	public const int KEY_9 = 57;
	public const int KEY_SEMICOLON = 59;
	public const int KEY_EQUAL = 61;
	public const int KEY_A = 65;
	public const int KEY_Z = 90;
	public const int KEY_LEFT_BRACKET = 91;
	public const int KEY_BACKSLASH = 92;
	public const int KEY_RIGHT_BRACKET = 93;
	public const int KEY_GRAVE = 96;
	public const int KEY_ESCAPE = 256;
	public const int KEY_ENTER = 257;
	public const int KEY_TAB = 258;
	public const int KEY_BACKSPACE = 259;
	public const int KEY_INSERT = 260;
	public const int KEY_DELETE = 261;
	public const int KEY_RIGHT = 262;
	public const int KEY_LEFT = 263;
	public const int KEY_DOWN = 264;
	public const int KEY_UP = 265;
	public const int KEY_PAGE_UP = 266;
	public const int KEY_PAGE_DOWN = 267;
	public const int KEY_HOME = 268;
	public const int KEY_END = 269;
	public const int KEY_CAPS_LOCK = 270;
	public const int KEY_PRINT_SCREEN = 273;
	public const int KEY_PAUSE = 274;
	public const int KEY_F1 = 290;
	public const int KEY_F12 = 301;
	public const int KEY_LEFT_SHIFT = 340;
	public const int KEY_LEFT_CONTROL = 341;
	public const int KEY_LEFT_ALT = 342;
	public const int KEY_RIGHT_SHIFT = 344;
	public const int KEY_RIGHT_CONTROL = 345;
	public const int KEY_RIGHT_ALT = 346;

	// joysticks
	public const int JOYSTICK_1 = 0;
	public const int GAMEPAD_AXIS_LEFT_X = 0;
	public const int GAMEPAD_AXIS_LEFT_Y = 1;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void ErrorCallback(int code, [MarshalAs(UnmanagedType.LPStr)] string description);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void KeyCallback(IntPtr window, int key, int scancode, int action, int mods);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void CharCallback(IntPtr window, uint codepoint);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void CursorPosCallback(IntPtr window, double x, double y);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void MouseButtonCallback(IntPtr window, int button, int action, int mods);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void ScrollCallback(IntPtr window, double xOffset, double yOffset);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void WindowFocusCallback(IntPtr window, int focused);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void FramebufferSizeCallback(IntPtr window, int width, int height);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void WindowCloseCallback(IntPtr window);

	[DllImport(Lib, EntryPoint = "glfwInit")]
	public static extern int Init();

	[DllImport(Lib, EntryPoint = "glfwTerminate")]
	public static extern void Terminate();

	[DllImport(Lib, EntryPoint = "glfwWindowHint")]
	public static extern void WindowHint(int hint, int value);

	[DllImport(Lib, EntryPoint = "glfwCreateWindow")]
	public static extern IntPtr CreateWindow(int width, int height,
		[MarshalAs(UnmanagedType.LPStr)] string title, IntPtr monitor, IntPtr share);

	[DllImport(Lib, EntryPoint = "glfwDestroyWindow")]
	public static extern void DestroyWindow(IntPtr window);

	[DllImport(Lib, EntryPoint = "glfwMakeContextCurrent")]
	public static extern void MakeContextCurrent(IntPtr window);

	[DllImport(Lib, EntryPoint = "glfwSwapBuffers")]
	public static extern void SwapBuffers(IntPtr window);

	[DllImport(Lib, EntryPoint = "glfwSwapInterval")]
	public static extern void SwapInterval(int interval);

	[DllImport(Lib, EntryPoint = "glfwPollEvents")]
	public static extern void PollEvents();

	[DllImport(Lib, EntryPoint = "glfwWindowShouldClose")]
	public static extern int WindowShouldClose(IntPtr window);

	[DllImport(Lib, EntryPoint = "glfwSetWindowShouldClose")]
	public static extern void SetWindowShouldClose(IntPtr window, int value);

	[DllImport(Lib, EntryPoint = "glfwGetTime")]
	public static extern double GetTime();

	[DllImport(Lib, EntryPoint = "glfwGetFramebufferSize")]
	public static extern void GetFramebufferSize(IntPtr window, out int width, out int height);

	[DllImport(Lib, EntryPoint = "glfwGetWindowSize")]
	public static extern void GetWindowSize(IntPtr window, out int width, out int height);

	[DllImport(Lib, EntryPoint = "glfwSetWindowSize")]
	public static extern void SetWindowSize(IntPtr window, int width, int height);

	[DllImport(Lib, EntryPoint = "glfwSetWindowTitle")]
	public static extern void SetWindowTitle(IntPtr window, [MarshalAs(UnmanagedType.LPStr)] string title);

	[DllImport(Lib, EntryPoint = "glfwGetWindowAttrib")]
	public static extern int GetWindowAttrib(IntPtr window, int attrib);

	[DllImport(Lib, EntryPoint = "glfwFocusWindow")]
	public static extern void FocusWindow(IntPtr window);

	[DllImport(Lib, EntryPoint = "glfwSetInputMode")]
	public static extern void SetInputMode(IntPtr window, int mode, int value);

	[DllImport(Lib, EntryPoint = "glfwSetCursorPos")]
	public static extern void SetCursorPos(IntPtr window, double x, double y);

	[DllImport(Lib, EntryPoint = "glfwSetClipboardString")]
	public static extern void SetClipboardString(IntPtr window, [MarshalAs(UnmanagedType.LPStr)] string text);

	[DllImport(Lib, EntryPoint = "glfwGetClipboardString")]
	public static extern IntPtr GetClipboardStringCore(IntPtr window);

	[DllImport(Lib, EntryPoint = "glfwSetErrorCallback")]
	public static extern IntPtr SetErrorCallback(ErrorCallback callback);

	[DllImport(Lib, EntryPoint = "glfwSetKeyCallback")]
	public static extern IntPtr SetKeyCallback(IntPtr window, KeyCallback callback);

	[DllImport(Lib, EntryPoint = "glfwSetCharCallback")]
	public static extern IntPtr SetCharCallback(IntPtr window, CharCallback callback);

	[DllImport(Lib, EntryPoint = "glfwSetCursorPosCallback")]
	public static extern IntPtr SetCursorPosCallback(IntPtr window, CursorPosCallback callback);

	[DllImport(Lib, EntryPoint = "glfwSetMouseButtonCallback")]
	public static extern IntPtr SetMouseButtonCallback(IntPtr window, MouseButtonCallback callback);

	[DllImport(Lib, EntryPoint = "glfwSetScrollCallback")]
	public static extern IntPtr SetScrollCallback(IntPtr window, ScrollCallback callback);

	[DllImport(Lib, EntryPoint = "glfwSetWindowFocusCallback")]
	public static extern IntPtr SetWindowFocusCallback(IntPtr window, WindowFocusCallback callback);

	[DllImport(Lib, EntryPoint = "glfwSetFramebufferSizeCallback")]
	public static extern IntPtr SetFramebufferSizeCallback(IntPtr window, FramebufferSizeCallback callback);

	[DllImport(Lib, EntryPoint = "glfwSetWindowCloseCallback")]
	public static extern IntPtr SetWindowCloseCallback(IntPtr window, WindowCloseCallback callback);

	[DllImport(Lib, EntryPoint = "glfwJoystickPresent")]
	public static extern int JoystickPresent(int jid);

	[DllImport(Lib, EntryPoint = "glfwGetJoystickAxes")]
	public static extern IntPtr GetJoystickAxes(int jid, out int count);

	[DllImport(Lib, EntryPoint = "glfwGetJoystickButtons")]
	public static extern IntPtr GetJoystickButtons(int jid, out int count);

	[DllImport(Lib, EntryPoint = "glfwGetJoystickName")]
	public static extern IntPtr GetJoystickNameCore(int jid);

	public static string GetClipboardString(IntPtr window)
	{
		IntPtr p = GetClipboardStringCore(window);
		return p == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(p) ?? string.Empty;
	}

	public static string GetJoystickName(int jid)
	{
		IntPtr p = GetJoystickNameCore(jid);
		return p == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(p) ?? string.Empty;
	}

	public static float[] GetAxes(int jid)
	{
		IntPtr p = GetJoystickAxes(jid, out int count);
		return ReadFloatArray(p, count);
	}

	public static byte[] GetButtons(int jid)
	{
		IntPtr p = GetJoystickButtons(jid, out int count);
		return ReadByteArray(p, count);
	}

	private static float[] ReadFloatArray(IntPtr p, int count)
	{
		if (p == IntPtr.Zero || count <= 0)
		{
			return Array.Empty<float>();
		}
		float[] result = new float[count];
		Marshal.Copy(p, result, 0, count);
		return result;
	}

	private static byte[] ReadByteArray(IntPtr p, int count)
	{
		if (p == IntPtr.Zero || count <= 0)
		{
			return Array.Empty<byte>();
		}
		byte[] result = new byte[count];
		Marshal.Copy(p, result, 0, count);
		return result;
	}
}
