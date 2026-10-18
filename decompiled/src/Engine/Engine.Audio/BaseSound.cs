using System;

namespace Engine.Audio;

public abstract class BaseSound : IDisposable
{
	private float m_volume = 1f;

	private float m_pitch = 1f;

	private float m_pan;

	internal object m_lock = new object();

	internal bool m_isLooped;

	internal bool m_disposeOnStop;

	internal int m_source;

	public SoundState State { get; internal set; }

	public int ChannelsCount { get; internal set; }

	public int SamplingFrequency { get; internal set; }

	public float Volume
	{
		get
		{
			return m_volume;
		}
		set
		{
			value = MathUtils.Saturate(value);
			if (value != m_volume)
			{
				m_volume = value;
				InternalSetVolume(value);
			}
		}
	}

	public float Pitch
	{
		get
		{
			return m_pitch;
		}
		set
		{
			value = MathUtils.Clamp(value, 0.5f, 2f);
			if (value != m_pitch)
			{
				m_pitch = value;
				InternalSetPitch(value);
			}
		}
	}

	public float Pan
	{
		get
		{
			return m_pan;
		}
		set
		{
			if (ChannelsCount == 1)
			{
				value = MathUtils.Clamp(value, -1f, 1f);
				if (value != m_pan)
				{
					m_pan = value;
					InternalSetPan(value);
				}
			}
		}
	}

	public bool IsLooped
	{
		get
		{
			return m_isLooped;
		}
		set
		{
			lock (m_lock)
			{
				if (State == SoundState.Stopped)
				{
					m_isLooped = value;
				}
			}
		}
	}

	public bool DisposeOnStop
	{
		get
		{
			return m_disposeOnStop;
		}
		set
		{
			lock (m_lock)
			{
				if (State == SoundState.Stopped)
				{
					m_disposeOnStop = value;
				}
			}
		}
	}

	public void Play()
	{
		lock (m_lock)
		{
			if (State == SoundState.Stopped || State == SoundState.Paused)
			{
				State = SoundState.Playing;
				InternalPlay();
			}
		}
	}

	public void Pause()
	{
		lock (m_lock)
		{
			if (State == SoundState.Playing)
			{
				State = SoundState.Paused;
				InternalPause();
			}
		}
	}

	public void Stop()
	{
		if (m_disposeOnStop)
		{
			Dispose();
		}
		lock (m_lock)
		{
			if (State == SoundState.Playing || State == SoundState.Paused)
			{
				State = SoundState.Stopped;
				InternalStop();
			}
		}
	}

	public virtual void Dispose()
	{
		if (State != SoundState.Disposed)
		{
			State = SoundState.Disposed;
			InternalDispose();
		}
	}

	internal unsafe BaseSound()
	{
		if (Mixer.m_isInitialized)
		{
			int source = 0;
			AL.GenSources(1, &source);
			Mixer.CheckALError();
			m_source = source;
		}
		Mixer.AddSound(this);
	}

	private void InternalSetVolume(float volume)
	{
		if (m_source != 0)
		{
			AL.Source(m_source, AL.Sourcef.Gain, volume);
			Mixer.CheckALError();
		}
	}

	private void InternalSetPitch(float pitch)
	{
		if (m_source != 0)
		{
			AL.Source(m_source, AL.Sourcef.Pitch, pitch);
			Mixer.CheckALError();
		}
	}

	private void InternalSetPan(float pan)
	{
		if (m_source != 0)
		{
			float value = 0f;
			float value2 = -0.1f;
			AL.Source(m_source, AL.Source3f.Position, pan, value, value2);
			Mixer.CheckALError();
		}
	}

	internal abstract void InternalPlay();

	internal abstract void InternalPause();

	internal abstract void InternalStop();

	internal unsafe virtual void InternalDispose()
	{
		if (m_source != 0)
		{
			AL.SourceStop(m_source);
			Mixer.CheckALError();
			int source = m_source;
			AL.DeleteSources(1, &source);
			Mixer.CheckALError();
			m_source = 0;
		}
		Mixer.RemoveSound(this);
	}
}
