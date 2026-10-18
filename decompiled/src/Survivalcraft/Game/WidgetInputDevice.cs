using System;

namespace Game;

[Flags]
public enum WidgetInputDevice
{
	None = 0,
	Keyboard = 1,
	MultiKeyboard1 = 2,
	MultiKeyboard2 = 4,
	MultiKeyboard3 = 8,
	MultiKeyboard4 = 0x10,
	Mouse = 0x20,
	MultiMouse1 = 0x40,
	MultiMouse2 = 0x80,
	MultiMouse3 = 0x100,
	MultiMouse4 = 0x200,
	Touch = 0x400,
	GamePad1 = 0x800,
	GamePad2 = 0x1000,
	GamePad3 = 0x2000,
	GamePad4 = 0x4000,
	VrControllers = 0x8000,
	MultiKeyboards = MultiKeyboard1 | MultiKeyboard2 | MultiKeyboard3 | MultiKeyboard4,
	MultiMice = MultiMouse1 | MultiMouse2 | MultiMouse3 | MultiMouse4,
	Gamepads = GamePad1 | GamePad2 | GamePad3 | GamePad4,
	All = Gamepads | Keyboard | Mouse | Touch | VrControllers
}
