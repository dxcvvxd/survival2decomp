using System.Runtime.InteropServices;
using System.Security;

namespace Engine.Audio;

[SuppressUnmanagedCodeSecurity]
internal static class AL
{
	public enum Error
	{
		NoError = 0,
		InvalidName = 40961,
		IllegalEnum = 40962,
		InvalidEnum = IllegalEnum,
		InvalidValue = 40963,
		IllegalCommand = 40964,
		InvalidOperation = IllegalCommand,
		OutOfMemory = 40965
	}

	public enum StringName
	{
		Extensions = 45060,
		Renderer = 45059,
		Vendor = 45057,
		Version = 45058
	}

	public enum DistModel
	{
		None = 0,
		InverseDistance = 53249,
		InverseDistanceClamped = 53250,
		LinearDistance = 53251,
		LinearDistanceClamped = 53252,
		ExponentDistance = 53253,
		ExponentDistanceClamped = 53254
	}

	public enum Listenerf
	{
		Gain = 4106,
		EfxMetersPerUnit = 131076
	}

	public enum Format
	{
		Mono8 = 4352,
		Mono16 = 4353,
		Stereo8 = 4354,
		Stereo16 = 4355,
		MonoALawExt = 65558,
		StereoALawExt = 65559,
		MonoMuLawExt = 65556,
		StereoMuLawExt = 65557,
		VorbisExt = 65539,
		Mp3Ext = 65568,
		MonoIma4Ext = 4864,
		StereoIma4Ext = 4865,
		MonoFloat32Ext = 65552,
		StereoFloat32Ext = 65553,
		MonoDoubleExt = 65554,
		StereoDoubleExt = 65555,
		Multi51Chn16Ext = 4619,
		Multi51Chn32Ext = 4620,
		Multi51Chn8Ext = 4618,
		Multi61Chn16Ext = 4622,
		Multi61Chn32Ext = 4623,
		Multi61Chn8Ext = 4621,
		Multi71Chn16Ext = 4625,
		Multi71Chn32Ext = 4626,
		Multi71Chn8Ext = 4624,
		MultiQuad16Ext = 4613,
		MultiQuad32Ext = 4614,
		MultiQuad8Ext = 4612,
		MultiRear16Ext = 4616,
		MultiRear32Ext = 4617,
		MultiRear8Ext = 4615
	}

	public enum GetSourcei
	{
		ByteOffset = 4134,
		SampleOffset = 4133,
		Buffer = 4105,
		SourceState = 4112,
		BuffersQueued = 4117,
		BuffersProcessed = 4118,
		SourceType = 4135
	}

	public enum Sourceb
	{
		SourceRelative = 514,
		Looping = 4103,
		EfxDirectFilterGainHighFrequencyAuto = 131082,
		EfxAuxiliarySendFilterGainAuto = 131083,
		EfxAuxiliarySendFilterGainHighFrequencyAuto = 131084
	}

	public enum Sourcei
	{
		ByteOffset = 4134,
		SampleOffset = 4133,
		Buffer = 4105,
		SourceType = 4135,
		EfxDirectFilter = 131077
	}

	public enum Sourcef
	{
		ReferenceDistance = 4128,
		MaxDistance = 4131,
		RolloffFactor = 4129,
		Pitch = 4099,
		Gain = 4106,
		MinGain = 4109,
		MaxGain = 4110,
		ConeInnerAngle = 4097,
		ConeOuterAngle = 4098,
		ConeOuterGain = 4130,
		SecOffset = 4132,
		EfxAirAbsorptionFactor = 131079,
		EfxRoomRolloffFactor = 131080,
		EfxConeOuterGainHighFrequency = 131081
	}

	public enum Source3f
	{
		Position = 4100,
		Velocity = 4102,
		Direction = 4101
	}

	public enum SourceState
	{
		Initial = 4113,
		Playing,
		Paused,
		Stopped
	}

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alGetError", ExactSpelling = true)]
	public static extern Error GetError();

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alGetString", ExactSpelling = true)]
	private static extern nint GetStringNative(StringName param);

	public static string GetString(StringName param)
	{
		return Marshal.PtrToStringAnsi(GetStringNative(param));
	}

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alGenBuffers", ExactSpelling = true)]
	public unsafe static extern void GenBuffers(int n, int* buffers);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alDeleteBuffers", ExactSpelling = true)]
	public unsafe static extern void DeleteBuffers(int n, int* buffers);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alBufferData", ExactSpelling = true)]
	public unsafe static extern void BufferData(int buffer, Format format, void* data, int size, int frequency);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alGenSources", ExactSpelling = true)]
	public unsafe static extern void GenSources(int n, int* sources);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alDeleteSources", ExactSpelling = true)]
	public unsafe static extern void DeleteSources(int n, int* sources);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alGetSourcei", ExactSpelling = true)]
	public static extern void GetSource(int source, GetSourcei param, out int value);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourcei", ExactSpelling = true)]
	public static extern void Source(int source, Sourceb param, bool value);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourcei", ExactSpelling = true)]
	public static extern void Source(int source, Sourcei param, int value);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourcef", ExactSpelling = true)]
	public static extern void Source(int source, Sourcef param, float value);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSource3f", ExactSpelling = true)]
	public static extern void Source(int source, Source3f param, float value1, float value2, float value3);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourceQueueBuffers", ExactSpelling = true)]
	public unsafe static extern void SourceQueueBuffers(int source, int n, int* buffers);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourceUnqueueBuffers", ExactSpelling = true)]
	public unsafe static extern void SourceUnqueueBuffers(int source, int n, int* buffers);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourcePlay", ExactSpelling = true)]
	public static extern void SourcePlay(int source);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourcePause", ExactSpelling = true)]
	public static extern void SourcePause(int source);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourceStop", ExactSpelling = true)]
	public static extern void SourceStop(int source);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alSourceRewind", ExactSpelling = true)]
	public static extern void SourceRewind(int source);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alDistanceModel", ExactSpelling = true)]
	public static extern void DistanceModel(DistModel distanceModel);

	[DllImport("libopenal.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "alListenerf", ExactSpelling = true)]
	public static extern void Listener(Listenerf param, float value);
}
