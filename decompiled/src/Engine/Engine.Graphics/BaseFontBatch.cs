using Engine.Media;

namespace Engine.Graphics;

public abstract class BaseFontBatch : BaseBatch
{
	internal static UnlitShader m_shader = new UnlitShader(useVertexColor: true, useTexture: true, useAdditiveColor: false, useAlphaThreshold: false);

	public readonly DynamicArray<VertexPositionColorTexture> TriangleVertices = new DynamicArray<VertexPositionColorTexture>();

	public readonly DynamicArray<ushort> TriangleIndices = new DynamicArray<ushort>();

	public BitmapFont Font { get; set; }

	public SamplerState SamplerState { get; set; }

	internal BaseFontBatch()
	{
	}

	public override bool IsEmpty()
	{
		return TriangleIndices.Count == 0;
	}

	public override void Clear()
	{
		TriangleVertices.Count = 0;
		TriangleIndices.Count = 0;
	}

	public void Flush(Matrix matrix, bool clearAfterFlush = true)
	{
		Flush(matrix, Vector4.One, clearAfterFlush);
	}

	public override void Flush(Matrix matrix, Vector4 color, bool clearAfterFlush = true)
	{
		Display.DepthStencilState = base.DepthStencilState;
		Display.RasterizerState = base.RasterizerState;
		Display.BlendState = base.BlendState;
		FlushWithDeviceState(Font, SamplerState, matrix, color, clearAfterFlush);
	}

	public void FlushWithDeviceState(BitmapFont font, SamplerState samplerState, Matrix matrix, Vector4 color, bool clearAfterFlush = true)
	{
		m_shader.Texture = font.Texture;
		m_shader.SamplerState = samplerState;
		m_shader.Transforms.World[0] = matrix;
		m_shader.Color = color;
		FlushWithDeviceState(m_shader, clearAfterFlush);
	}

	public void FlushWithDeviceState(Shader shader, bool clearAfterFlush = true)
	{
		if (TriangleIndices.Count > 0)
		{
			int num = 0;
			int num2 = TriangleIndices.Count;
			while (num2 > 0)
			{
				int num3 = MathUtils.Min(num2, 196605);
				Display.DrawUserIndexed(PrimitiveType.TriangleList, shader, VertexPositionColorTexture.VertexDeclaration, TriangleVertices.Array, 0, TriangleVertices.Count, TriangleIndices.Array, num, num3);
				num += num3;
				num2 -= num3;
			}
		}
		if (clearAfterFlush)
		{
			Clear();
		}
	}

	public void TransformTriangles(Matrix matrix, int start = 0, int end = -1)
	{
		VertexPositionColorTexture[] array = TriangleVertices.Array;
		if (end < 0)
		{
			end = TriangleVertices.Count;
		}
		for (int i = start; i < end; i++)
		{
			Vector3.Transform(ref array[i].Position, ref matrix, out array[i].Position);
		}
	}

	public void TransformTrianglesColors(Color color, int start = 0, int end = -1)
	{
		VertexPositionColorTexture[] array = TriangleVertices.Array;
		if (end < 0)
		{
			end = TriangleVertices.Count;
		}
		for (int i = start; i < end; i++)
		{
			array[i].Color *= color;
		}
	}

	protected Vector2 CalculateTextOffset(string text, int start, int count, TextAnchor anchor, Vector2 scale, Vector2 spacing)
	{
		Vector2 zero = Vector2.Zero;
		if (anchor != TextAnchor.Default)
		{
			Vector2 vector = Font.MeasureText(text, start, count, scale, spacing);
			if ((anchor & TextAnchor.HorizontalCenter) != TextAnchor.Default)
			{
				zero.X = (0f - vector.X) / 2f;
			}
			else if ((anchor & TextAnchor.Right) != TextAnchor.Default)
			{
				zero.X = 0f - vector.X;
			}
			if ((anchor & TextAnchor.VerticalCenter) != TextAnchor.Default)
			{
				zero.Y = (0f - vector.Y) / 2f;
			}
			else if ((anchor & TextAnchor.Bottom) != TextAnchor.Default)
			{
				zero.Y = 0f - vector.Y;
			}
		}
		return zero;
	}
}
