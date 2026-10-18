using System.Runtime.InteropServices;
using System.Security;

namespace Engine.Audio;

[SuppressUnmanagedCodeSecurity]
internal static class Alc
{
	public enum Error
	{
		NoError = 0,
		InvalidDevice = 40961,
		InvalidContext = 40962,
		InvalidEnum = 40963,
		InvalidValue = 40964,
		OutOfMemory = 40965
	}

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "alcOpenDevice", ExactSpelling = true)]
	public static extern nint OpenDevice(string deviceName);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "alcCloseDevice", ExactSpelling = true)]
	public static extern bool CloseDevice(nint device);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alcCreateContext", ExactSpelling = true)]
	public unsafe static extern nint CreateContext(nint device, int* attrlist);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alcDestroyContext", ExactSpelling = true)]
	public static extern void DestroyContext(nint context);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alcMakeContextCurrent", ExactSpelling = true)]
	public static extern bool MakeContextCurrent(nint context);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alcGetError", ExactSpelling = true)]
	public static extern Error GetError(nint device);
}
