using System.IO;
using Engine.Graphics;
using Engine.Media;
using Engine.Serialization;

namespace Engine.Content;

[ContentReader("Engine.Graphics.Texture2D")]
public class TextureContentReader : IContentReader
{
	public object Read(ContentStream stream, object existingObject, string name)
	{
		Texture2D texture2D;
		if (existingObject == null)
		{
			texture2D = ReadTexture(stream);
		}
		else
		{
			texture2D = (Texture2D)existingObject;
			LoadTextureData(stream, texture2D);
		}
		texture2D.DebugName = name;
		return texture2D;
	}

	public static Texture2D ReadTexture(Stream stream)
	{
		return LoadTextureData(stream, null);
	}

	private static Texture2D LoadTextureData(Stream stream, Texture2D texture)
	{
		EngineBinaryReader engineBinaryReader = new EngineBinaryReader(stream);
		bool flag = engineBinaryReader.ReadBoolean();
		int num = engineBinaryReader.ReadByte();
		Image image = ImageContentReader.ReadImage(stream);
		if (texture == null)
		{
			texture = new Texture2D(image.Width, image.Height, num, ColorFormat.Rgba8888);
		}
		else if (texture.Width != image.Width || texture.Height != image.Height || texture.MipLevelsCount != num)
		{
			throw new InvalidDataException("Existing texture size or mipmapLevelsCount doesn't match");
		}
		texture.SetData(0, image.Pixels);
		if (flag)
		{
			texture.Tag = image;
		}
		for (int i = 1; i < texture.MipLevelsCount; i++)
		{
			image = ImageContentReader.ReadImage(stream);
			texture.SetData(i, image.Pixels);
		}
		return texture;
	}
}
