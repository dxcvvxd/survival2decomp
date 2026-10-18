using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Input;

/// <summary>
/// Linux GamePad: GLFW joystick polling instead of Android MotionEvent input.
/// Public API is identical to the original.
/// </summary>
public static class GamePad
{
	private class State
	{
		public bool IsConnected;

		public Vector2[] Sticks = new Vector2[2];

		public float[] Triggers = new float[2];

		public bool[] Buttons = new bool[14];

		public bool[] LastButtons = new bool[14];

		public double[] ButtonsRepeat = new double[14];
	}

	private static Dictionary<int, int> m_deviceToIndex = new Dictionary<int, int>();

	private static List<int> m_toRemove = new List<int>();

	private const double m_buttonFirstRepeatTime = 0.2;

	private const double m_buttonNextRepeatTime = 0.04;

	private static State[] m_states = new State[4]
	{
		new State(),
		new State(),
		new State(),
		new State()
	};

	internal static void Initialize()
	{
	}

	internal static void Dispose()
	{
	}

	internal static void BeforeFrame()
	{
		if (!Time.PeriodicEvent(0.5, 0.0))
		{
			return;
		}
		for (int jid = 0; jid < 4; jid++)
		{
			bool present = Glfw.JoystickPresent(jid) != 0;
			int deviceId = 1000 + jid;
			if (present)
			{
				if (!m_deviceToIndex.ContainsKey(deviceId))
				{
					int index = FindFreeIndex();
					if (index < 0)
					{
						continue;
					}
					Connect(deviceId, index);
				}
				PollState(jid, deviceId);
			}
			else if (m_deviceToIndex.ContainsKey(deviceId))
			{
				Disconnect(deviceId);
			}
		}
	}

	private static int FindFreeIndex()
	{
		for (int i = 0; i < m_states.Length; i++)
		{
			if (!m_deviceToIndex.Values.Contains(i))
			{
				return i;
			}
		}
		return -1;
	}

	private static void PollState(int jid, int deviceId)
	{
		if (!m_deviceToIndex.TryGetValue(deviceId, out int index))
		{
			return;
		}
		State state = m_states[index];
		float[] axes = Glfw.GetAxes(jid);
		byte[] buttons = Glfw.GetButtons(jid);

		// standard Linux/XInput axis layout: 0 LX, 1 LY, 2 RX, 3 RY, 4 LT, 5 RT (when present)
		if (axes.Length >= 2)
		{
			state.Sticks[0] = new Vector2(ClampAxis(axes[0]), -ClampAxis(axes[1]));
		}
		if (axes.Length >= 4)
		{
			state.Sticks[1] = new Vector2(ClampAxis(axes[2]), -ClampAxis(axes[3]));
		}
		if (axes.Length >= 6)
		{
			state.Triggers[0] = ToTrigger(axes[4]);
			state.Triggers[1] = ToTrigger(axes[5]);
		}
		else if (axes.Length >= 5)
		{
			state.Triggers[0] = ToTrigger(axes[4]);
		}

		// button layout: 0 A, 1 B, 2 X, 3 Y, 4 LB, 5 RB, 6 Back, 7 Start, 8 LS, 9 RS
		SetButton(state, buttons, 0, GamePadButton.A);
		SetButton(state, buttons, 1, GamePadButton.B);
		SetButton(state, buttons, 2, GamePadButton.X);
		SetButton(state, buttons, 3, GamePadButton.Y);
		SetButton(state, buttons, 4, GamePadButton.LeftShoulder);
		SetButton(state, buttons, 5, GamePadButton.RightShoulder);
		SetButton(state, buttons, 6, GamePadButton.Back);
		SetButton(state, buttons, 7, GamePadButton.Start);
		SetButton(state, buttons, 8, GamePadButton.LeftThumb);
		SetButton(state, buttons, 9, GamePadButton.RightThumb);
		// dpad hats are commonly exposed as buttons 11..14 on Linux gamepads
		if (buttons.Length > 14)
		{
			SetButton(state, buttons, 11, GamePadButton.DPadLeft);
			SetButton(state, buttons, 12, GamePadButton.DPadUp);
			SetButton(state, buttons, 13, GamePadButton.DPadRight);
			SetButton(state, buttons, 14, GamePadButton.DPadDown);
		}
	}

	private static void SetButton(State state, byte[] buttons, int sourceIndex, GamePadButton button)
	{
		if (sourceIndex < buttons.Length)
		{
			state.Buttons[(int)button] = buttons[sourceIndex] != 0;
		}
	}

	private static float ClampAxis(float value)
	{
		return MathUtils.Clamp(value, -1f, 1f);
	}

	private static float ToTrigger(float value)
	{
		// triggers report -1..1 or 0..1 depending on driver
		if (value >= 0f)
		{
			return MathUtils.Clamp(value, 0f, 1f);
		}
		return 0f;
	}

	private static void Connect(int deviceId, int index)
	{
		m_deviceToIndex[deviceId] = index;
		m_states[index].IsConnected = true;
	}

	private static void Disconnect(int deviceId)
	{
		if (m_deviceToIndex.TryGetValue(deviceId, out var value))
		{
			m_deviceToIndex.Remove(deviceId);
			m_states[value].IsConnected = false;
			m_states[value].Sticks[0] = Vector2.Zero;
			m_states[value].Sticks[1] = Vector2.Zero;
			m_states[value].Triggers[0] = 0f;
			m_states[value].Triggers[1] = 0f;
			Array.Clear(m_states[value].Buttons);
		}
	}

	public static bool IsConnected(int gamePadIndex)
	{
		if (gamePadIndex < 0 || gamePadIndex >= m_states.Length)
		{
			throw new ArgumentOutOfRangeException("gamePadIndex");
		}
		return m_states[gamePadIndex].IsConnected;
	}

	public static Vector2 GetStickPosition(int gamePadIndex, GamePadStick stick, float deadZone = 0f)
	{
		if (deadZone < 0f || deadZone >= 1f)
		{
			throw new ArgumentOutOfRangeException("deadZone");
		}
		if (IsConnected(gamePadIndex))
		{
			Vector2 result = m_states[gamePadIndex].Sticks[(int)stick];
			if (deadZone > 0f)
			{
				float num = result.Length();
				if (num > 0f)
				{
					float num2 = ApplyDeadZone(num, deadZone);
					result *= num2 / num;
				}
			}
			return result;
		}
		return Vector2.Zero;
	}

	public static float GetTriggerPosition(int gamePadIndex, GamePadTrigger trigger, float deadZone = 0f)
	{
		if (deadZone < 0f || deadZone >= 1f)
		{
			throw new ArgumentOutOfRangeException("deadZone");
		}
		if (IsConnected(gamePadIndex))
		{
			return ApplyDeadZone(m_states[gamePadIndex].Triggers[(int)trigger], deadZone);
		}
		return 0f;
	}

	public static bool IsButtonDown(int gamePadIndex, GamePadButton button)
	{
		if (IsConnected(gamePadIndex))
		{
			return m_states[gamePadIndex].Buttons[(int)button];
		}
		return false;
	}

	public static bool IsButtonDownOnce(int gamePadIndex, GamePadButton button)
	{
		if (IsConnected(gamePadIndex))
		{
			if (m_states[gamePadIndex].Buttons[(int)button])
			{
				return !m_states[gamePadIndex].LastButtons[(int)button];
			}
			return false;
		}
		return false;
	}

	public static bool IsButtonUpOnce(int gamePadIndex, GamePadButton button)
	{
		if (IsConnected(gamePadIndex))
		{
			if (!m_states[gamePadIndex].Buttons[(int)button])
			{
				return m_states[gamePadIndex].LastButtons[(int)button];
			}
			return false;
		}
		return false;
	}

	public static bool IsButtonDownRepeat(int gamePadIndex, GamePadButton button)
	{
		if (IsConnected(gamePadIndex))
		{
			if (m_states[gamePadIndex].Buttons[(int)button] && !m_states[gamePadIndex].LastButtons[(int)button])
			{
				return true;
			}
			double num = m_states[gamePadIndex].ButtonsRepeat[(int)button];
			if (num != 0.0)
			{
				return Time.FrameStartTime >= num;
			}
			return false;
		}
		return false;
	}

	public static void Clear()
	{
		for (int i = 0; i < m_states.Length; i++)
		{
			for (int j = 0; j < m_states[i].Sticks.Length; j++)
			{
				m_states[i].Sticks[j] = Vector2.Zero;
			}
			for (int k = 0; k < m_states[i].Triggers.Length; k++)
			{
				m_states[i].Triggers[k] = 0f;
			}
			for (int l = 0; l < m_states[i].Buttons.Length; l++)
			{
				m_states[i].Buttons[l] = false;
				m_states[i].ButtonsRepeat[l] = 0.0;
			}
		}
	}

	internal static void AfterFrame()
	{
		for (int i = 0; i < m_states.Length; i++)
		{
			if (Keyboard.BackButtonQuitsApp && IsButtonDownOnce(i, GamePadButton.Back))
			{
				Window.Close();
			}
			State state = m_states[i];
			for (int j = 0; j < state.Buttons.Length; j++)
			{
				if (state.Buttons[j])
				{
					if (!state.LastButtons[j])
					{
						state.ButtonsRepeat[j] = Time.FrameStartTime + 0.2;
					}
					else if (Time.FrameStartTime >= state.ButtonsRepeat[j])
					{
						state.ButtonsRepeat[j] = MathUtils.Max(Time.FrameStartTime, state.ButtonsRepeat[j] + 0.04);
					}
				}
				else
				{
					state.ButtonsRepeat[j] = 0.0;
				}
				state.LastButtons[j] = state.Buttons[j];
			}
		}
	}

	private static float ApplyDeadZone(float value, float deadZone)
	{
		return MathUtils.Sign(value) * MathUtils.Max(MathUtils.Abs(value) - deadZone, 0f) / (1f - deadZone);
	}
}
