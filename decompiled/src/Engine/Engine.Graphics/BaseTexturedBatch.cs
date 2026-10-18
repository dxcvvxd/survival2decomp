namespace Engine.Graphics;

public abstract class BaseTexturedBatch : BaseBatch
{
	internal static UnlitShader m_shader = new UnlitShader(useVertexColor: true, useTexture: true, useAdditiveColor: false, useAlphaThreshold: false);

	internal static UnlitShader m_shaderAlphaTest = new UnlitShader(useVertexColor: true, useTexture: true, useAdditiveColor: false, useAlphaThreshold: true);

	public readonly DynamicArray<VertexPositionColorTexture> TriangleVertices = new DynamicArray<VertexPositionColorTexture>();

	public readonly DynamicArray<ushort> TriangleIndices = new DynamicArray<ushort>();

	public Texture2D Texture { get; set; }

	public bool UseAlphaTest { get; set; }

	public SamplerState SamplerState { get; set; }

	internal BaseTexturedBatch()
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
		FlushWithDeviceState(UseAlphaTest, Texture, SamplerState, matrix, color, clearAfterFlush);
	}

	public void FlushWithDeviceState(bool useAlphaTest, Texture2D texture, SamplerState samplerState, Matrix matrix, Vector4 color, bool clearAfterFlush = true)
	{
		if (useAlphaTest)
		{
			m_shaderAlphaTest.Texture = texture;
			m_shaderAlphaTest.SamplerState = samplerState;
			m_shaderAlphaTest.Transforms.World[0] = matrix;
			m_shaderAlphaTest.Color = color;
			m_shaderAlphaTest.AlphaThreshold = 0f;
			FlushWithDeviceState(m_shaderAlphaTest, clearAfterFlush);
		}
		else
		{
			m_shader.Texture = texture;
			m_shader.SamplerState = samplerState;
			m_shader.Transforms.World[0] = matrix;
			m_shader.Color = color;
			FlushWithDeviceState(m_shader, clearAfterFlush);
		}
	}

	public void FlushWithDeviceState(Shader shader, bool clearAfterFlush = true)
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
}
