using System;
using System.IO;
using Engine.Media;
using Engine.Serialization;

namespace Engine.Content;

[ContentReader("Engine.Media.Image")]
public class ImageContentReader : IContentReader
{
	public object Read(ContentStream stream, object existingObject, string name)
	{
		if (existingObject == null)
		{
			return ReadImage(stream);
		}
		throw new NotSupportedException();
	}

	public static Image ReadImage(Stream stream)
	{
		ImageFileFormat imageFileFormat = (ImageFileFormat)stream.ReadByte();
		if (imageFileFormat == ImageFileFormat.Jpg)
		{
			EngineBinaryReader engineBinaryReader = new EngineBinaryReader(stream);
			int count = engineBinaryReader.ReadInt32();
			return Image.Load(new MemoryStream(engineBinaryReader.ReadBytes(count)), imageFileFormat);
		}
		return Image.Load(stream, imageFileFormat);
	}
}
