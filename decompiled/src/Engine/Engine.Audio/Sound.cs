using System;

namespace Engine.Audio;

public sealed class Sound : BaseSound
{
	private SoundBuffer m_soundBuffer;

	public SoundBuffer SoundBuffer => m_soundBuffer;

	public override void Dispose()
	{
		base.Dispose();
		if (m_soundBuffer != null)
		{
			SoundBuffer soundBuffer = m_soundBuffer;
			int useCount = soundBuffer.UseCount - 1;
			soundBuffer.UseCount = useCount;
			m_soundBuffer = null;
		}
	}

	internal void Initialize(SoundBuffer soundBuffer)
	{
		if (soundBuffer == null)
		{
			throw new ArgumentNullException("soundBuffer");
		}
		m_soundBuffer = soundBuffer;
		SoundBuffer soundBuffer2 = m_soundBuffer;
		int useCount = soundBuffer2.UseCount + 1;
		soundBuffer2.UseCount = useCount;
	}

	public Sound(SoundBuffer soundBuffer, float volume = 1f, float pitch = 1f, float pan = 0f, bool isLooped = false, bool disposeOnStop = false)
	{
		if (soundBuffer == null)
		{
			throw new ArgumentNullException("soundBuffer");
		}
		if (Mixer.m_isInitialized)
		{
			AL.Source(m_source, AL.Sourcei.Buffer, soundBuffer.m_buffer);
			Mixer.CheckALError();
		}
		Initialize(soundBuffer);
		base.ChannelsCount = soundBuffer.ChannelsCount;
		base.SamplingFrequency = soundBuffer.SamplingFrequency;
		base.Volume = volume;
		base.Pitch = pitch;
		base.Pan = pan;
		base.IsLooped = isLooped;
		base.DisposeOnStop = disposeOnStop;
	}

	internal override void InternalPlay()
	{
		if (m_source != 0)
		{
			AL.Source(m_source, AL.Sourceb.Looping, m_isLooped);
			AL.SourcePlay(m_source);
			Mixer.CheckALError();
		}
	}

	internal override void InternalPause()
	{
		if (m_source != 0)
		{
			AL.SourcePause(m_source);
			Mixer.CheckALError();
		}
	}

	internal override void InternalStop()
	{
		if (m_source != 0)
		{
			AL.SourceRewind(m_source);
			Mixer.CheckALError();
		}
	}
}
