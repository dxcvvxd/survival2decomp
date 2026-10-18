using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine.Media;
using Engine.Serialization;

namespace Engine.Content;

[ContentWriter("Engine.Media.BitmapFont")]
public class BitmapFontContentWriter : IContentWriter
{
	private class BitmapFontData
	{
		public List<BitmapFont.Glyph> Glyphs = new List<BitmapFont.Glyph>();
	}

	private struct KerningPair
	{
		public char C1;

		public char C2;

		public float Kerning;
	}

	public string Font;

	[Optional]
	public int FirstCode = 32;

	[Optional]
	public int FallbackCode = 95;

	[Optional]
	public Vector2 Spacing = Vector2.Zero;

	[Optional]
	public float Scale = 1f;

	[Optional]
	public Vector2 Offset = Vector2.Zero;

	[Optional]
	public float MinWidth;

	[Optional]
	public int KerningLimit = -1;

	[Optional]
	public int KerningBulkingRadius = 1;

	[Optional]
	public float KerningBulkingGradient = 1f;

	[Optional]
	public int KerningTolerance = 1;

	[Optional]
	public bool GenerateMipmaps;

	[Optional]
	public int MipmapsCount = int.MaxValue;

	[Optional]
	public bool PremultiplyAlpha = true;

	[Optional]
	public ImageFileFormat ImageFileFormat;

	public IEnumerable<string> GetDependencies()
	{
		yield return Font;
	}

	public void Write(string projectDirectory, Stream stream)
	{
		Image image = Image.Load(Storage.OpenFile(Storage.CombinePaths(projectDirectory, Font), OpenFileMode.Read), Image.DetermineFileFormat(Storage.GetExtension(Font)));
		BitmapFont.KerningSettings kerningSettings = null;
		if (KerningLimit >= 0)
		{
			kerningSettings = new BitmapFont.KerningSettings
			{
				Limit = KerningLimit,
				BulkingRadius = KerningBulkingRadius,
				BulkingGradient = KerningBulkingGradient,
				Tolerance = KerningTolerance
			};
		}
		WriteBitmapFont(stream, image, (char)FirstCode, (char)FallbackCode, Spacing, Scale, Offset, MinWidth, kerningSettings, (!GenerateMipmaps) ? 1 : MipmapsCount, PremultiplyAlpha, ImageFileFormat);
	}

	public static void WriteBitmapFont(Stream stream, Image image, char firstCode, char fallbackCode, Vector2 spacing, float scale, Vector2 offset, float minWidth, BitmapFont.KerningSettings kerningSettings, int mipmapsCount, bool premultiplyAlpha, ImageFileFormat imageFormat)
	{
		EngineBinaryWriter engineBinaryWriter = new EngineBinaryWriter(stream);
		BitmapFont bitmapFont = BitmapFont.InternalLoad(image, firstCode, fallbackCode, spacing, scale, minWidth, offset, kerningSettings, 1, premultiplyAlpha, createTexture: false);
		engineBinaryWriter.Write(bitmapFont.m_glyphsByCode.Count((BitmapFont.Glyph g) => g != null));
		for (int num = 0; num < bitmapFont.m_glyphsByCode.Length; num++)
		{
			BitmapFont.Glyph glyph = bitmapFont.m_glyphsByCode[num];
			if (glyph != null)
			{
				engineBinaryWriter.Write(glyph.Code);
				engineBinaryWriter.Write(glyph.TexCoord1);
				engineBinaryWriter.Write(glyph.TexCoord2);
				engineBinaryWriter.Write(glyph.Offset);
				engineBinaryWriter.Write(glyph.Width);
			}
		}
		engineBinaryWriter.Write(bitmapFont.GlyphHeight);
		engineBinaryWriter.Write(bitmapFont.Spacing);
		engineBinaryWriter.Write(bitmapFont.Scale);
		engineBinaryWriter.Write(bitmapFont.FallbackGlyph.Code);
		List<KerningPair> list = new List<KerningPair>();
		for (char c = '\0'; c <= bitmapFont.MaxGlyphCode; c = (char)(c + 1))
		{
			for (char c2 = '\0'; c2 <= bitmapFont.MaxGlyphCode; c2 = (char)(c2 + 1))
			{
				float kerning = bitmapFont.GetKerning(c, c2);
				if (kerning != 0f)
				{
					list.Add(new KerningPair
					{
						C1 = c,
						C2 = c2,
						Kerning = kerning
					});
				}
			}
		}
		engineBinaryWriter.Write(list.Count);
		foreach (KerningPair item in list)
		{
			engineBinaryWriter.Write7BitEncodedInt(item.C1);
			engineBinaryWriter.Write7BitEncodedInt(item.C2);
			engineBinaryWriter.Write7BitEncodedInt((int)item.Kerning);
		}
		TextureContentWriter.WriteTexture(stream, bitmapFont.m_image, mipmapsCount, premultiplyAlpha: false, keepSourceImageInTag: false, imageFormat);
	}
}
