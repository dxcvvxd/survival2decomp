using Engine.Media;

namespace Engine.Graphics;

public class FontBatch3D : BaseFontBatch
{
	public FontBatch3D()
	{
		base.Font = BitmapFont.DebugFont;
		base.DepthStencilState = DepthStencilState.Default;
		base.RasterizerState = RasterizerState.CullNoneScissor;
		base.BlendState = BlendState.AlphaBlend;
		base.SamplerState = SamplerState.LinearClamp;
	}

	public void QueueText(string text, Vector3 position, Vector3 right, Vector3 down, Color color, TextAnchor anchor = TextAnchor.Default, Vector2 spacing = default(Vector2))
	{
		Vector2 vector = CalculateTextOffset(scale: new Vector2(right.Length(), down.Length()), text: text, start: 0, count: text.Length, anchor: anchor, spacing: spacing);
		Vector3 vector2 = position + vector.X * Vector3.Normalize(right) + vector.Y * Vector3.Normalize(down);
		Vector3 vector3 = vector2;
		right *= base.Font.Scale;
		down *= base.Font.Scale;
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '\u00a0')
			{
				c = ' ';
			}
			switch (c)
			{
			case '\n':
				num++;
				vector3 = vector2 + (float)num * (base.Font.GlyphHeight + base.Font.Spacing.Y + spacing.Y) * down;
				continue;
			case '\r':
			case '\u200b':
				continue;
			}
			BitmapFont.Glyph glyph = base.Font.GetGlyph(c);
			if (!glyph.IsBlank)
			{
				Vector3 vector4 = right * (glyph.TexCoord2.X - glyph.TexCoord1.X) * base.Font.Texture.Width;
				Vector3 vector5 = down * (glyph.TexCoord2.Y - glyph.TexCoord1.Y) * base.Font.Texture.Height;
				Vector3 vector6 = right * glyph.Offset.X + down * glyph.Offset.Y;
				Vector3 vector7 = vector3 + vector6;
				Vector3 vector8 = vector7 + vector4;
				Vector3 vector9 = vector7 + vector5;
				Vector3 vector10 = vector7 + vector4 + vector5;
				int count = TriangleVertices.Count;
				TriangleVertices.Count += 4;
				TriangleVertices.Array[count] = new VertexPositionColorTexture(new Vector3(vector7.X, vector7.Y, vector7.Z), color, new Vector2(glyph.TexCoord1.X, glyph.TexCoord1.Y));
				TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(new Vector3(vector8.X, vector8.Y, vector8.Z), color, new Vector2(glyph.TexCoord2.X, glyph.TexCoord1.Y));
				TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(new Vector3(vector10.X, vector10.Y, vector10.Z), color, new Vector2(glyph.TexCoord2.X, glyph.TexCoord2.Y));
				TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(new Vector3(vector9.X, vector9.Y, vector9.Z), color, new Vector2(glyph.TexCoord1.X, glyph.TexCoord2.Y));
				int count2 = TriangleIndices.Count;
				TriangleIndices.Count += 6;
				TriangleIndices.Array[count2] = (ushort)count;
				TriangleIndices.Array[count2 + 1] = (ushort)(count + 1);
				TriangleIndices.Array[count2 + 2] = (ushort)(count + 2);
				TriangleIndices.Array[count2 + 3] = (ushort)(count + 2);
				TriangleIndices.Array[count2 + 4] = (ushort)(count + 3);
				TriangleIndices.Array[count2 + 5] = (ushort)count;
			}
			float num2 = ((i < text.Length - 1) ? base.Font.GetKerning(c, text[i + 1]) : 0f);
			vector3 += right * (glyph.Width - num2 + base.Font.Spacing.X + spacing.X);
		}
	}
}
