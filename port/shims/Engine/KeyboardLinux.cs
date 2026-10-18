using System;

namespace Engine.Input;

/// <summary>
/// Linux replacement for the Android Keyboard: fed by GLFW key/char callbacks.
/// Public API is identical to the original.
/// </summary>
public static class Keyboard
{
	private const double m_keyFirstRepeatTime = 0.3;

	private const double m_keyNextRepeatTime = 0.04;

	private static bool[] m_keysDownArray = new bool[Enum.GetValues(typeof(Key)).Length];

	private static bool[] m_keysDownOnceArray = new bool[Enum.GetValues(typeof(Key)).Length];

	private static double[] m_keysDownRepeatArray = new double[Enum.GetValues(typeof(Key)).Length];

	private static Key? m_lastKey;

	private static char? m_lastChar;

	public static Key? LastKey => m_lastKey;

	public static char? LastChar => m_lastChar;

	public static bool IsKeyboardVisible { get; private set; }

	public static bool BackButtonQuitsApp { get; set; }

	public static event Action<Key> KeyDown;

	public static event Action<Key> KeyUp;

	public static event Action<char> CharacterEntered;

	internal static void Initialize()
	{
	}

	internal static void Dispose()
	{
	}

	internal static void HandleGlfwKeyDown(int glfwKey)
	{
		Key key = TranslateGlfwKey(glfwKey);
		if (key != (Key)(-1))
		{
			ProcessKeyDown(key);
		}
	}

	internal static void HandleGlfwKeyUp(int glfwKey)
	{
		Key key = TranslateGlfwKey(glfwKey);
		if (key != (Key)(-1))
		{
			ProcessKeyUp(key);
		}
	}

	internal static void HandleGlfwChar(uint codepoint)
	{
		if (codepoint <= 31 || codepoint == 127)
		{
			return;
		}
		char ch = (char)codepoint;
		ProcessCharacterEntered(ch);
	}

	private static Key TranslateGlfwKey(int glfwKey)
	{
		if (glfwKey >= Glfw.KEY_A && glfwKey <= Glfw.KEY_Z)
		{
			return (Key)((int)Key.A + (glfwKey - Glfw.KEY_A));
		}
		if (glfwKey >= Glfw.KEY_0 && glfwKey <= Glfw.KEY_9)
		{
			return (Key)((int)Key.Number0 + (glfwKey - Glfw.KEY_0));
		}
		if (glfwKey >= Glfw.KEY_F1 && glfwKey <= Glfw.KEY_F12)
		{
			return (Key)((int)Key.F1 + (glfwKey - Glfw.KEY_F1));
		}
		return glfwKey switch
		{
			Glfw.KEY_SPACE => Key.Space,
			Glfw.KEY_COMMA => Key.Comma,
			Glfw.KEY_PERIOD => Key.Period,
			Glfw.KEY_SLASH => Key.Slash,
			Glfw.KEY_SEMICOLON => Key.Semicolon,
			Glfw.KEY_APOSTROPHE => Key.Quote,
			Glfw.KEY_LEFT_BRACKET => Key.LeftBracket,
			Glfw.KEY_RIGHT_BRACKET => Key.RightBracket,
			Glfw.KEY_GRAVE => Key.Tilde,
			Glfw.KEY_MINUS => Key.Minus,
			Glfw.KEY_EQUAL => Key.Plus,
			Glfw.KEY_ESCAPE => Key.Escape,
			Glfw.KEY_ENTER => Key.Enter,
			Glfw.KEY_TAB => Key.Tab,
			Glfw.KEY_BACKSPACE => Key.BackSpace,
			Glfw.KEY_INSERT => Key.Insert,
			Glfw.KEY_DELETE => Key.Delete,
			Glfw.KEY_RIGHT => Key.RightArrow,
			Glfw.KEY_LEFT => Key.LeftArrow,
			Glfw.KEY_DOWN => Key.DownArrow,
			Glfw.KEY_UP => Key.UpArrow,
			Glfw.KEY_PAGE_UP => Key.PageUp,
			Glfw.KEY_PAGE_DOWN => Key.PageDown,
			Glfw.KEY_HOME => Key.Home,
			Glfw.KEY_END => Key.End,
			Glfw.KEY_CAPS_LOCK => Key.CapsLock,
			Glfw.KEY_LEFT_SHIFT => Key.Shift,
			Glfw.KEY_RIGHT_SHIFT => Key.Shift,
			Glfw.KEY_LEFT_CONTROL => Key.Control,
			Glfw.KEY_RIGHT_CONTROL => Key.Control,
			Glfw.KEY_LEFT_ALT => Key.Alt,
			Glfw.KEY_RIGHT_ALT => Key.Alt,
			_ => (Key)(-1),
		};
	}

	public static bool IsKeyDown(Key key)
	{
		return m_keysDownArray[(int)key];
	}

	public static bool IsKeyDownOnce(Key key)
	{
		return m_keysDownOnceArray[(int)key];
	}

	public static bool IsKeyDownRepeat(Key key)
	{
		double num = m_keysDownRepeatArray[(int)key];
		if (!(num < 0.0))
		{
			if (num != 0.0)
			{
				return Time.FrameStartTime >= num;
			}
			return false;
		}
		return true;
	}

	public static void ShowKeyboard(string title, string description, string defaultText, bool passwordMode, Action<string> enter, Action cancel)
	{
		if (title == null)
		{
			throw new ArgumentNullException("title");
		}
		if (description == null)
		{
			throw new ArgumentNullException("description");
		}
		if (defaultText == null)
		{
			throw new ArgumentNullException("defaultText");
		}
		if (IsKeyboardVisible)
		{
			return;
		}
		// No OS keyboard on desktop: TextBoxWidget edits inline (its Update() handles
		// typing/caret/Enter while focused), so simply keep focus and do nothing here.
		// Flows that rely solely on the dialog callback (e.g. furniture set naming)
		// need a native input dialog — added in a later polish phase.
		Log.Debug($"ShowKeyboard(\"{title}\") not supported yet on desktop; keeping inline input.");
	}

	public static void Clear()
	{
		m_lastKey = null;
		m_lastChar = null;
		for (int i = 0; i < m_keysDownArray.Length; i++)
		{
			m_keysDownArray[i] = false;
			m_keysDownOnceArray[i] = false;
			m_keysDownRepeatArray[i] = 0.0;
		}
	}

	internal static void BeforeFrame()
	{
	}

	internal static void AfterFrame()
	{
		if (BackButtonQuitsApp && IsKeyDownOnce(Key.Back))
		{
			Window.Close();
		}
		m_lastKey = null;
		m_lastChar = null;
		for (int i = 0; i < m_keysDownOnceArray.Length; i++)
		{
			m_keysDownOnceArray[i] = false;
		}
		for (int j = 0; j < m_keysDownRepeatArray.Length; j++)
		{
			if (m_keysDownArray[j])
			{
				if (m_keysDownRepeatArray[j] < 0.0)
				{
					m_keysDownRepeatArray[j] = Time.FrameStartTime + 0.3;
				}
				else if (Time.FrameStartTime >= m_keysDownRepeatArray[j])
				{
					m_keysDownRepeatArray[j] = MathUtils.Max(Time.FrameStartTime, m_keysDownRepeatArray[j] + 0.04);
				}
			}
			else
			{
				m_keysDownRepeatArray[j] = 0.0;
			}
		}
	}

	private static bool ProcessKeyDown(Key key)
	{
		if (!Window.IsActive || IsKeyboardVisible)
		{
			return false;
		}
		m_lastKey = key;
		if (!m_keysDownArray[(int)key])
		{
			m_keysDownArray[(int)key] = true;
			m_keysDownOnceArray[(int)key] = true;
			m_keysDownRepeatArray[(int)key] = -1.0;
		}
		KeyDown?.Invoke(key);
		return true;
	}

	private static bool ProcessKeyUp(Key key)
	{
		if (!Window.IsActive || IsKeyboardVisible)
		{
			return false;
		}
		if (m_keysDownArray[(int)key])
		{
			m_keysDownArray[(int)key] = false;
		}
		KeyUp?.Invoke(key);
		return true;
	}

	private static bool ProcessCharacterEntered(char ch)
	{
		if (!Window.IsActive || IsKeyboardVisible)
		{
			return false;
		}
		m_lastChar = ch;
		CharacterEntered?.Invoke(ch);
		return true;
	}
}
