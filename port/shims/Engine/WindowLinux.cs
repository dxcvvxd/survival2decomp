using System;
using System.Threading.Tasks;
using Engine.Audio;
using Engine.Graphics;
using Engine.Input;
using Engine.Media;

namespace Engine;

/// <summary>
/// Linux replacement for the Android Window: GLFW window with an OpenGL ES 2 context.
/// Public API mirrors the original so game code compiles unchanged.
/// </summary>
public static class Window
{
	private enum State
	{
		Uncreated,
		Inactive,
		Active
	}

	private static int m_presentationInterval = 1;

	private static double m_frameStartTime;

	private static float m_scale = 1f;

	private static State m_state = State.Uncreated;

	private static IntPtr m_window;

	private static bool m_firstFrame = true;

	/// <summary>Startup focus retention: keep demanding X input focus until the main UI is up.</summary>
	private static long m_focusKeepUntilMs;

	private static long m_nextFocusAttemptMs;

	// keep delegates alive for the lifetime of the window
	private static Glfw.KeyCallback s_keyCallback;
	private static Glfw.CharCallback s_charCallback;
	private static Glfw.CursorPosCallback s_cursorPosCallback;
	private static Glfw.MouseButtonCallback s_mouseButtonCallback;
	private static Glfw.ScrollCallback s_scrollCallback;
	private static Glfw.WindowFocusCallback s_focusCallback;
	private static Glfw.FramebufferSizeCallback s_framebufferSizeCallback;
	private static Glfw.WindowCloseCallback s_closeCallback;
	private static Glfw.ErrorCallback s_errorCallback;

	/// <summary>Native GLFW window handle (used by input shims).</summary>
	internal static IntPtr Handle => m_window;

	public static Point2 ScreenSize
	{
		get
		{
			if (m_window == IntPtr.Zero)
			{
				return Point2.Zero;
			}
			Glfw.GetFramebufferSize(m_window, out int w, out int h);
			return new Point2(w, h);
		}
	}

	public static WindowMode WindowMode
	{
		get
		{
			VerifyWindowOpened();
			return WindowMode.Resizable;
		}
		set
		{
			VerifyWindowOpened();
		}
	}

	public static Point2 Position
	{
		get
		{
			VerifyWindowOpened();
			return Point2.Zero;
		}
		set
		{
			VerifyWindowOpened();
		}
	}

	public static Point2 Size
	{
		get
		{
			VerifyWindowOpened();
			Glfw.GetWindowSize(m_window, out int w, out int h);
			return new Point2(w, h);
		}
		set
		{
			VerifyWindowOpened();
			Glfw.SetWindowSize(m_window, value.X, value.Y);
		}
	}

	public static float Scale
	{
		get
		{
			VerifyWindowOpened();
			return m_scale;
		}
	}

	public static string Title
	{
		get
		{
			VerifyWindowOpened();
			return string.Empty;
		}
		set
		{
			VerifyWindowOpened();
			if (m_window != IntPtr.Zero && value != null)
			{
				Glfw.SetWindowTitle(m_window, value);
			}
		}
	}

	public static Image Icon
	{
		get
		{
			VerifyWindowOpened();
			return null;
		}
		set
		{
			VerifyWindowOpened();
		}
	}

	public static int PresentationInterval
	{
		get
		{
			VerifyWindowOpened();
			return m_presentationInterval;
		}
		set
		{
			VerifyWindowOpened();
			m_presentationInterval = MathUtils.Clamp(value, 1, 4);
		}
	}

	public static string ClipboardString
	{
		get
		{
			VerifyWindowOpened();
			return Glfw.GetClipboardString(m_window);
		}
		set
		{
			VerifyWindowOpened();
			Glfw.SetClipboardString(m_window, value ?? string.Empty);
		}
	}

	public static bool IsCreated => m_state != State.Uncreated;

	public static bool IsActive => m_state == State.Active;

	public static event Action Created;

	public static event Action Resized;

	public static event Action Activated;

	public static event Action Deactivated;

	public static event Action Closed;

	public static event Action BeforeFrame;

	public static event Action Frame;

	public static event Action AfterFrame;

	public static event Action<UnhandledExceptionInfo> UnhandledException;

	public static event Action<Uri> HandleUri;

	public static event Action LowMemory;

	public static void Run(int width = 0, int height = 0, WindowMode windowMode = WindowMode.Fullscreen, string title = "")
	{
		if (m_window != IntPtr.Zero)
		{
			throw new InvalidOperationException("Window is already opened.");
		}
		AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs args)
		{
			if (UnhandledException != null)
			{
				Exception ex = args.ExceptionObject as Exception;
				if (ex == null)
				{
					ex = new Exception($"Unknown exception. Additional information: {args.ExceptionObject}");
				}
				UnhandledException(new UnhandledExceptionInfo(ex));
			}
		};
		Log.Information("Host.OS: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
		Log.Information("Host.ProcessArchitecture: " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);

		s_errorCallback = delegate(int code, string description)
		{
			Log.Warning($"GLFW error 0x{code:X}: {description}");
		};
		Glfw.SetErrorCallback(s_errorCallback);

		if (Glfw.Init() == 0)
		{
			throw new InvalidOperationException("Failed to initialize GLFW.");
		}

		Glfw.WindowHint(Glfw.CLIENT_API, Glfw.OPENGL_ES_API);
		Glfw.WindowHint(Glfw.CONTEXT_VERSION_MAJOR, 2);
		Glfw.WindowHint(Glfw.CONTEXT_VERSION_MINOR, 0);
		Glfw.WindowHint(Glfw.RESIZABLE, (windowMode == WindowMode.Resizable) ? 1 : 0);
		Glfw.WindowHint(Glfw.VISIBLE, 1);
		Glfw.WindowHint(Glfw.FOCUSED, 1);
		Glfw.WindowHint(Glfw.FOCUS_ON_SHOW, 1);

		if (width <= 0 || height <= 0)
		{
			width = 1280;
			height = 720;
		}
		if (string.IsNullOrEmpty(title))
		{
			title = "Survivalcraft 2";
		}
		m_window = Glfw.CreateWindow(width, height, title, IntPtr.Zero, IntPtr.Zero);
		if (m_window == IntPtr.Zero)
		{
			Glfw.Terminate();
			throw new InvalidOperationException("Failed to create OpenGL ES 2 window.");
		}
		Glfw.MakeContextCurrent(m_window);
		Glfw.SwapInterval(1);
		m_scale = 1f;

		// The window must hold input focus from launch until the main UI is up:
		// some WMs map the window unfocused or steal focus transiently during
		// loading, which would leave the game without keyboard/mouse input.
		m_focusKeepUntilMs = Environment.TickCount64 + 15000;
		m_nextFocusAttemptMs = 0L;

		s_keyCallback = (window, key, scancode, action, mods) =>
		{
			if (action == Glfw.PRESS || action == Glfw.REPEAT)
			{
				Keyboard.HandleGlfwKeyDown(key);
			}
			else if (action == Glfw.RELEASE)
			{
				Keyboard.HandleGlfwKeyUp(key);
			}
		};
		s_charCallback = (window, codepoint) => Keyboard.HandleGlfwChar(codepoint);
		s_cursorPosCallback = (window, x, y) =>
		{
			Mouse.HandleGlfwCursorPos(x, y);
		};
		s_mouseButtonCallback = (window, button, action, mods) =>
		{
			bool pressed = action == Glfw.PRESS;
			Mouse.GetLastCursorPos(out double x, out double y);
			Mouse.HandleGlfwMouseButton(button, pressed, x, y);
		};
		s_scrollCallback = (window, xOffset, yOffset) => Mouse.HandleGlfwScroll(yOffset);
		s_focusCallback = (window, focused) =>
		{
			// No pause on unfocus: the game keeps running (and sounding) in the
			// background. Only drop held keys so nothing sticks while unfocused.
			if (focused != 0)
			{
				ResumedHandler();
			}
			else
			{
				Keyboard.Clear();
			}
		};
		s_framebufferSizeCallback = (window, w, h) => ResizeHandler();
		s_closeCallback = window => { };

		Glfw.SetKeyCallback(m_window, s_keyCallback);
		Glfw.SetCharCallback(m_window, s_charCallback);
		Glfw.SetCursorPosCallback(m_window, s_cursorPosCallback);
		Glfw.SetMouseButtonCallback(m_window, s_mouseButtonCallback);
		Glfw.SetScrollCallback(m_window, s_scrollCallback);
		Glfw.SetWindowFocusCallback(m_window, s_focusCallback);
		Glfw.SetFramebufferSizeCallback(m_window, s_framebufferSizeCallback);
		Glfw.SetWindowCloseCallback(m_window, s_closeCallback);

		try
		{
			while (Glfw.WindowShouldClose(m_window) == 0)
			{
				Glfw.PollEvents();
				MaintainStartupFocus();
				RenderFrameHandler();
			}
		}
		finally
		{
			DestroyedHandler();
			Glfw.DestroyWindow(m_window);
			m_window = IntPtr.Zero;
			Glfw.Terminate();
		}
	}

	public static void Close()
	{
		VerifyWindowOpened();
		if (m_window != IntPtr.Zero)
		{
			Glfw.SetWindowShouldClose(m_window, 1);
		}
	}

	private static void VerifyWindowOpened()
	{
		if (m_window == IntPtr.Zero)
		{
			throw new InvalidOperationException("Window is not opened.");
		}
	}

	/// <summary>
	/// During the startup window (launch → main UI), re-demand input focus whenever
	/// the WM reports the window unfocused. Throttled to one attempt per 500 ms;
	/// stops entirely after the retention period or once the window is focused.
	/// </summary>
	private static void MaintainStartupFocus()
	{
		long num = Environment.TickCount64;
		if (num > m_focusKeepUntilMs)
		{
			return;
		}
		if (num < m_nextFocusAttemptMs)
		{
			return;
		}
		if (Glfw.GetWindowAttrib(m_window, Glfw.FOCUSED) != 0)
		{
			return;
		}
		Glfw.FocusWindow(m_window);
		m_nextFocusAttemptMs = num + 500;
	}

	private static void ResumedHandler()
	{
		if (m_state == State.Inactive)
		{
			m_state = State.Active;
			Mixer.Activate();
			Activated?.Invoke();
		}
	}

	private static void DestroyedHandler()
	{
		if (m_state == State.Active || m_state == State.Inactive)
		{
			if (m_state == State.Active)
			{
				Deactivated?.Invoke();
			}
			m_state = State.Uncreated;
			Closed?.Invoke();
			DisposeAll();
		}
	}

	private static void ResizeHandler()
	{
		if (m_state != State.Uncreated)
		{
			Display.Resize();
			Resized?.Invoke();
		}
	}

	private static void RenderFrameHandler()
	{
		if (m_firstFrame)
		{
			m_firstFrame = false;
			InitializeAll();
			m_state = State.Inactive;
			Created?.Invoke();
			m_state = State.Active;
			Activated?.Invoke();
		}
		if (m_state != State.Active)
		{
			return;
		}
		ProcessFrame();
		Glfw.SwapBuffers(m_window);
		if (m_presentationInterval >= 2)
		{
			double num = Time.RealTime - m_frameStartTime;
			int num2 = (int)(1000.0 * ((double)((float)m_presentationInterval / 60f) - num));
			if (num2 > 0)
			{
				Task.Delay(num2).Wait();
			}
		}
		m_frameStartTime = Time.RealTime;
	}

	private static void InitializeAll()
	{
		Dispatcher.Initialize();
		Display.Initialize();
		Keyboard.Initialize();
		MultiKeyboard.Initialize();
		Mouse.Initialize();
		MultiMouse.Initialize();
		Touch.Initialize();
		GamePad.Initialize();
		Mixer.Initialize();
	}

	private static void DisposeAll()
	{
		Display.Dispose();
		Keyboard.Dispose();
		MultiKeyboard.Dispose();
		Mouse.Dispose();
		MultiMouse.Dispose();
		Touch.Dispose();
		GamePad.Dispose();
		Mixer.Dispose();
	}

	private static void BeforeFrameAll()
	{
		Time.BeforeFrame();
		Dispatcher.BeforeFrame();
		Display.BeforeFrame();
		Keyboard.BeforeFrame();
		MultiKeyboard.BeforeFrame();
		Mouse.BeforeFrame();
		MultiMouse.BeforeFrame();
		Touch.BeforeFrame();
		GamePad.BeforeFrame();
		Mixer.BeforeFrame();
	}

	private static void AfterFrameAll()
	{
		Time.AfterFrame();
		Dispatcher.AfterFrame();
		Display.AfterFrame();
		Keyboard.AfterFrame();
		MultiKeyboard.AfterFrame();
		Mouse.AfterFrame();
		MultiMouse.AfterFrame();
		Touch.AfterFrame();
		GamePad.AfterFrame();
		Mixer.AfterFrame();
	}

	private static void ProcessFrame()
	{
		BeforeFrame?.Invoke();
		BeforeFrameAll();
		try
		{
			Frame?.Invoke();
		}
		finally
		{
			AfterFrameAll();
			AfterFrame?.Invoke();
		}
	}
}
