using System.Collections.Generic;
using System.IO;
using Engine.Media;
using Engine.Serialization;

namespace Engine.Content;

[ContentWriter("Engine.Graphics.Texture2D")]
public class TextureContentWriter : IContentWriter
{
	public string Texture;

	[Optional]
	public bool KeepSourceImageDataInTag;

	[Optional]
	public bool GenerateMipmaps = true;

	[Optional]
	public int MipmapsCount = int.MaxValue;

	[Optional]
	public bool PremultiplyAlpha;

	[Optional]
	public ImageFileFormat ImageFileFormat;

	public IEnumerable<string> GetDependencies()
	{
		yield return Texture;
	}

	public void Write(string projectDirectory, Stream stream)
	{
		Image image = Image.Load(Storage.OpenFile(Storage.CombinePaths(projectDirectory, Texture), OpenFileMode.Read), Image.DetermineFileFormat(Storage.GetExtension(Texture)));
		WriteTexture(stream, image, (!GenerateMipmaps) ? 1 : MipmapsCount, PremultiplyAlpha, KeepSourceImageDataInTag, ImageFileFormat);
	}

	public static void WriteTexture(Stream stream, Image image, int mipmapsCount, bool premultiplyAlpha, bool keepSourceImageInTag, ImageFileFormat format)
	{
		if (premultiplyAlpha)
		{
			image = new Image(image);
			Image.PremultiplyAlpha(image);
		}
		List<Image> list = new List<Image>();
		if (mipmapsCount > 1)
		{
			list.AddRange(Image.GenerateMipmaps(image, mipmapsCount));
		}
		else
		{
			list.Add(image);
		}
		EngineBinaryWriter engineBinaryWriter = new EngineBinaryWriter(stream);
		engineBinaryWriter.Write(keepSourceImageInTag);
		engineBinaryWriter.Write((byte)list.Count);
		foreach (Image item in list)
		{
			ImageContentWriter.WriteImage(stream, item, format);
		}
	}
}
