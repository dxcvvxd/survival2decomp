using System;
using Engine.Media;

namespace Engine.Content;

[ContentReader("Engine.Media.StreamingSource")]
public class StreamingSourceContentReader : IContentReader
{
	public object Read(ContentStream stream, object existingObject, string name)
	{
		if (existingObject == null)
		{
			SoundFileFormat format = SoundData.DetermineFileFormat(stream);
			return SoundData.Stream(stream.Duplicate(), format);
		}
		throw new NotSupportedException();
	}
}
