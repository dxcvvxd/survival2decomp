using System.Collections.Generic;
using System.IO;
using Engine.Media;
using Engine.Serialization;

namespace Engine.Content;

[ContentWriter("Engine.Media.Image")]
public class ImageContentWriter : IContentWriter
{
	public string Image;

	[Optional]
	public ImageFileFormat ImageFileFormat;

	public IEnumerable<string> GetDependencies()
	{
		yield return Image;
	}

	public void Write(string projectDirectory, Stream stream)
	{
		Image image = Engine.Media.Image.Load(Storage.CombinePaths(projectDirectory, Image));
		WriteImage(stream, image, ImageFileFormat);
	}

	public static void WriteImage(Stream stream, Image image, ImageFileFormat format)
	{
		stream.WriteByte((byte)format);
		if (format == ImageFileFormat.Jpg)
		{
			MemoryStream memoryStream = new MemoryStream();
			Engine.Media.Image.Save(image, memoryStream, format, saveAlpha: true);
			memoryStream.Position = 0L;
			new EngineBinaryWriter(stream).Write((int)memoryStream.Length);
			memoryStream.CopyTo(stream);
		}
		else
		{
			Engine.Media.Image.Save(image, stream, format, saveAlpha: true);
		}
	}
}
