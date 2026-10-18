using System;
using System.Collections.Generic;

namespace Engine.Audio;

public static class Mixer
{
	private static float m_masterVolume = 1f;

	private static nint m_device;

	private static nint m_context;

	internal static bool m_isInitialized;

	private static HashSet<BaseSound> m_sounds = new HashSet<BaseSound>();

	private static List<BaseSound> m_soundsToStop = new List<BaseSound>();

	private static List<BaseSound> m_pausedSounds = new List<BaseSound>();

	public static string DeviceDescription { get; internal set; }

	public static float MasterVolume
	{
		get
		{
			return m_masterVolume;
		}
		set
		{
			value = MathUtils.Saturate(value);
			if (value != m_masterVolume)
			{
				m_masterVolume = value;
				InternalSetMasterVolume(value);
			}
		}
	}

	internal unsafe static void Initialize()
	{
		try
		{
			m_device = Alc.OpenDevice(null);
			CheckAlcError(m_device);
			m_context = Alc.CreateContext(m_device, null);
			CheckAlcError(m_device);
			Alc.MakeContextCurrent(m_context);
			CheckAlcError(m_device);
			AL.DistanceModel(AL.DistModel.None);
			CheckALError();
			m_isInitialized = true;
		}
		catch (Exception value)
		{
			Log.Error($"Error intializing OpenAL, audio will be disabled. Reason: {value}");
		}
		DeviceDescription = $"OpenAL, Vendor={AL.GetString(AL.StringName.Vendor)}, Renderer={AL.GetString(AL.StringName.Renderer)}, Version={AL.GetString(AL.StringName.Version)}";
		Log.Information("Initialized audio device: " + DeviceDescription);
	}

	internal static void Dispose()
	{
		Alc.MakeContextCurrent(IntPtr.Zero);
		CheckAlcError(m_device);
		if (m_context != IntPtr.Zero)
		{
			Alc.DestroyContext(m_context);
			CheckAlcError(m_device);
			m_context = IntPtr.Zero;
		}
		if (m_device != IntPtr.Zero)
		{
			Alc.CloseDevice(m_device);
			m_device = IntPtr.Zero;
		}
	}

	internal static void Activate()
	{
		foreach (BaseSound pausedSound in m_pausedSounds)
		{
			pausedSound.Play();
		}
	}

	internal static void Deactivate()
	{
		lock (m_sounds)
		{
			m_pausedSounds.Clear();
			foreach (BaseSound sound in m_sounds)
			{
				if (sound.State == SoundState.Playing)
				{
					sound.Pause();
					m_pausedSounds.Add(sound);
				}
			}
		}
	}

	internal static void BeforeFrame()
	{
		lock (m_sounds)
		{
			foreach (BaseSound sound in m_sounds)
			{
				if (sound is Sound && sound.m_source != 0 && sound.State == SoundState.Playing)
				{
					AL.GetSource(sound.m_source, AL.GetSourcei.SourceState, out var value);
					if (value == 4116)
					{
						m_soundsToStop.Add(sound);
					}
				}
			}
		}
		foreach (BaseSound item in m_soundsToStop)
		{
			item.Stop();
		}
		m_soundsToStop.Clear();
	}

	internal static void AfterFrame()
	{
	}

	internal static void InternalSetMasterVolume(float volume)
	{
		if (m_isInitialized)
		{
			AL.Listener(AL.Listenerf.Gain, volume);
		}
	}

	internal static void CheckAlcError(nint device)
	{
		if (device != IntPtr.Zero)
		{
			Alc.Error error = Alc.GetError(device);
			if (error != Alc.Error.NoError)
			{
				throw new InvalidOperationException(error.ToString());
			}
		}
	}

	internal static void CheckALError()
	{
		AL.Error error = AL.GetError();
		if (error != AL.Error.NoError)
		{
			throw new InvalidOperationException(error.ToString());
		}
	}

	internal static void AddSound(BaseSound sound)
	{
		lock (m_sounds)
		{
			m_sounds.Add(sound);
		}
	}

	internal static void RemoveSound(BaseSound sound)
	{
		lock (m_sounds)
		{
			m_sounds.Remove(sound);
		}
	}
}
