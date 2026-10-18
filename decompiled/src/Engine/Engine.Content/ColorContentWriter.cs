using System.Collections.Generic;
using System.IO;
using Engine.Serialization;

namespace Engine.Content;

[ContentWriter("Engine.Color")]
public class ColorContentWriter : IContentWriter
{
	public Color Color;

	public IEnumerable<string> GetDependencies()
	{
		yield break;
	}

	public void Write(string projectDirectory, Stream stream)
	{
		new EngineBinaryWriter(stream).Write(Color);
	}
}
