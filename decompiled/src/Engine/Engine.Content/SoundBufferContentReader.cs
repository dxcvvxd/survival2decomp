using System;
using System.IO;
using Engine.Audio;
using Engine.Media;

namespace Engine.Content;

[ContentReader("Engine.Audio.SoundBuffer")]
public class SoundBufferContentReader : IContentReader
{
	public object Read(ContentStream stream, object existingObject, string name)
	{
		if (existingObject == null)
		{
			BinaryReader binaryReader = new BinaryReader(stream);
			bool flag = binaryReader.ReadBoolean();
			bool flag2 = binaryReader.ReadBoolean();
			int channelsCount = binaryReader.ReadInt32();
			int samplingFrequency = binaryReader.ReadInt32();
			int bytesCount = binaryReader.ReadInt32();
			SoundData soundData;
			if (flag2)
			{
				MemoryStream memoryStream = new MemoryStream();
				using StreamingSource streamingSource = Ogg.Stream(stream);
				streamingSource.CopyTo(memoryStream);
				if (memoryStream.Length > int.MaxValue)
				{
					throw new InvalidOperationException("Audio data too long.");
				}
				memoryStream.Position = 0L;
				soundData = new SoundData(memoryStream, (int)memoryStream.Length, channelsCount, samplingFrequency);
			}
			else
			{
				soundData = new SoundData(stream, bytesCount, channelsCount, samplingFrequency);
			}
			SoundBuffer soundBuffer = SoundBuffer.Load(soundData);
			if (flag)
			{
				soundBuffer.Tag = soundData;
			}
			return soundBuffer;
		}
		throw new NotSupportedException();
	}
}
