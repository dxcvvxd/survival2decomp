using System;
using Engine.Serialization;

namespace Engine.Content;

[ContentReader("Engine.Color")]
public class ColorContentReader : IContentReader
{
	public object Read(ContentStream stream, object existingObject, string name)
	{
		if (existingObject == null)
		{
			return new EngineBinaryReader(stream).ReadColor();
		}
		throw new NotSupportedException();
	}
}
