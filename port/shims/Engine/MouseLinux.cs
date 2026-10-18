using System;

namespace Engine.Input;

/// <summary>
/// Linux replacement for the Android Mouse: fed by GLFW cursor/button/scroll callbacks.
/// Public API is identical to the original.
/// </summary>
public static class Mouse
{
	private static Vector2 m_queuedMouseMovement;

	private static float m_queuedMouseWheelMovement;

	private static bool m_pointerCaptureRequested;

	private static bool[] m_mouseButtonsDownArray;

	private static int[] m_mouseButtonsDownFrameArray;

	private static bool[] m_mouseButtonsDelayedUpArray;

	private static bool[] m_mouseButtonsDownOnceArray;

	private static bool[] m_mouseButtonsUpOnceArray;

	private static double m_lastCursorX;

	private static double m_lastCursorY;

	public static Point2 MouseMovement { get; private set; }

	public static int MouseWheelMovement { get; private set; }

	public static Point2? MousePosition { get; private set; }

	public static bool IsMouseVisible { get; set; }

	public static event Action<MouseEvent> MouseMove;

	public static event Action<MouseButtonEvent> MouseDown;

	public static event Action<MouseButtonEvent> MouseUp;

	public static void SetMousePosition(int x, int y)
	{
		if (Window.Handle != IntPtr.Zero)
		{
			Glfw.SetCursorPos(Window.Handle, x, y);
			m_lastCursorX = x;
			m_lastCursorY = y;
			MousePosition = Round(x, y);
		}
	}

	internal static void GetLastCursorPos(out double x, out double y)
	{
		x = m_lastCursorX;
		y = m_lastCursorY;
	}

	internal static void Initialize()
	{
	}

	internal static void Dispose()
	{
	}

	internal static void BeforeFrame()
	{
		if (IsMouseVisible)
		{
			if (m_pointerCaptureRequested)
			{
				m_pointerCaptureRequested = false;
				if (Window.Handle != IntPtr.Zero)
				{
					Glfw.SetInputMode(Window.Handle, Glfw.CURSOR, Glfw.CURSOR_NORMAL);
				}
			}
		}
		else
		{
			if (!m_pointerCaptureRequested)
			{
				m_pointerCaptureRequested = true;
				if (Window.Handle != IntPtr.Zero)
				{
					Glfw.SetInputMode(Window.Handle, Glfw.CURSOR, Glfw.CURSOR_DISABLED);
				}
			}
			MouseMovement = Round(m_queuedMouseMovement.X, m_queuedMouseMovement.Y);
			m_queuedMouseMovement = Vector2.Zero;
		}
		MouseWheelMovement = (int)MathUtils.Round(m_queuedMouseWheelMovement) * 120;
		m_queuedMouseWheelMovement = 0f;
	}

	internal static void HandleGlfwCursorPos(double x, double y)
	{
		double dx = x - m_lastCursorX;
		double dy = y - m_lastCursorY;
		m_lastCursorX = x;
		m_lastCursorY = y;
		if (!IsMouseVisible && Window.IsActive)
		{
			// cursor is disabled: GLFW reports virtual positions, accumulate as relative movement
			m_queuedMouseMovement += new Vector2((float)dx, (float)dy);
		}
		if (IsMouseVisible)
		{
			MousePosition = Round((float)x, (float)y);
		}
	}

	internal static void HandleGlfwMouseButton(int glfwButton, bool pressed, double x, double y)
	{
		MouseButton button = TranslateMouseButton(glfwButton);
		Point2 position = Round((float)x, (float)y);
		if (pressed)
		{
			ProcessMouseDown(button, position);
		}
		else
		{
			ProcessMouseUp(button, position);
		}
	}

	internal static void HandleGlfwScroll(double yOffset)
	{
		m_queuedMouseWheelMovement += MathUtils.Sign((float)yOffset);
	}

	private static MouseButton TranslateMouseButton(int glfwButton)
	{
		return glfwButton switch
		{
			Glfw.MOUSE_BUTTON_RIGHT => MouseButton.Right,
			Glfw.MOUSE_BUTTON_MIDDLE => MouseButton.Middle,
			_ => MouseButton.Left,
		};
	}

	private static Point2 Round(float x, float y)
	{
		return new Point2((int)MathUtils.Round(x), (int)MathUtils.Round(y));
	}

	static Mouse()
	{
		m_mouseButtonsDownArray = new bool[Enum.GetValues(typeof(MouseButton)).Length];
		m_mouseButtonsDownFrameArray = new int[Enum.GetValues(typeof(MouseButton)).Length];
		m_mouseButtonsDelayedUpArray = new bool[Enum.GetValues(typeof(MouseButton)).Length];
		m_mouseButtonsDownOnceArray = new bool[Enum.GetValues(typeof(MouseButton)).Length];
		m_mouseButtonsUpOnceArray = new bool[Enum.GetValues(typeof(MouseButton)).Length];
		IsMouseVisible = true;
	}

	public static bool IsMouseButtonDown(MouseButton mouseButton)
	{
		return m_mouseButtonsDownArray[(int)mouseButton];
	}

	public static bool IsMouseButtonDownOnce(MouseButton mouseButton)
	{
		return m_mouseButtonsDownOnceArray[(int)mouseButton];
	}

	public static bool IsMouseButtonUpOnce(MouseButton mouseButton)
	{
		return m_mouseButtonsUpOnceArray[(int)mouseButton];
	}

	public static void Clear()
	{
		for (int i = 0; i < m_mouseButtonsDownArray.Length; i++)
		{
			m_mouseButtonsDownArray[i] = false;
			m_mouseButtonsDownFrameArray[i] = 0;
			m_mouseButtonsDelayedUpArray[i] = false;
			m_mouseButtonsDownOnceArray[i] = false;
			m_mouseButtonsUpOnceArray[i] = false;
		}
	}

	internal static void AfterFrame()
	{
		for (int i = 0; i < m_mouseButtonsDownArray.Length; i++)
		{
			m_mouseButtonsDownOnceArray[i] = false;
			if (m_mouseButtonsDelayedUpArray[i])
			{
				m_mouseButtonsDelayedUpArray[i] = false;
				m_mouseButtonsDownArray[i] = false;
				m_mouseButtonsUpOnceArray[i] = true;
			}
			else
			{
				m_mouseButtonsUpOnceArray[i] = false;
			}
		}
		if (!IsMouseVisible)
		{
			MousePosition = null;
		}
	}

	private static void ProcessMouseDown(MouseButton mouseButton, Point2 position)
	{
		if (Window.IsActive && !Keyboard.IsKeyboardVisible)
		{
			if (!MousePosition.HasValue)
			{
				ProcessMouseMove(position);
			}
			m_mouseButtonsDownArray[(int)mouseButton] = true;
			m_mouseButtonsDownFrameArray[(int)mouseButton] = Time.FrameIndex;
			m_mouseButtonsDownOnceArray[(int)mouseButton] = true;
			if (IsMouseVisible && MouseDown != null)
			{
				MouseDown(new MouseButtonEvent
				{
					Button = mouseButton,
					Position = position
				});
			}
		}
	}

	private static void ProcessMouseUp(MouseButton mouseButton, Point2 position)
	{
		if (Window.IsActive && !Keyboard.IsKeyboardVisible)
		{
			if (!MousePosition.HasValue)
			{
				ProcessMouseMove(position);
			}
			if (m_mouseButtonsDownArray[(int)mouseButton] && Time.FrameIndex == m_mouseButtonsDownFrameArray[(int)mouseButton])
			{
				m_mouseButtonsDelayedUpArray[(int)mouseButton] = true;
			}
			else
			{
				m_mouseButtonsDownArray[(int)mouseButton] = false;
				m_mouseButtonsUpOnceArray[(int)mouseButton] = true;
			}
			if (IsMouseVisible && MouseUp != null)
			{
				MouseUp(new MouseButtonEvent
				{
					Button = mouseButton,
					Position = position
				});
			}
		}
	}

	private static void ProcessMouseMove(Point2 position)
	{
		if (Window.IsActive && !Keyboard.IsKeyboardVisible && IsMouseVisible)
		{
			MousePosition = position;
			if (MouseMove != null)
			{
				MouseMove(new MouseEvent
				{
					Position = position
				});
			}
		}
	}
}
