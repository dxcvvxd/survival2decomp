using System;
using System.Runtime.InteropServices;
using Engine.Graphics.Bindings;

namespace Engine.Graphics;

public static class Display
{
	private static DisplayStatistics m_statistics;

	private static RenderTarget2D m_renderTarget;

	private static RasterizerState m_rasterizerState = RasterizerState.CullCounterClockwise;

	private static DepthStencilState m_depthStencilState = DepthStencilState.Default;

	private static BlendState m_blendState = BlendState.Opaque;

	public static DisplayStatistics Statistics
	{
		get
		{
			m_statistics.FrameIndex = Time.FrameIndex;
			return m_statistics;
		}
	}

	public static Point2 BackbufferSize { get; private set; }

	public static Viewport Viewport { get; set; }

	public static Rectangle ScissorRectangle { get; set; }

	public static RasterizerState RasterizerState
	{
		get
		{
			return m_rasterizerState;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			m_rasterizerState = value;
			value.IsLocked = true;
		}
	}

	public static DepthStencilState DepthStencilState
	{
		get
		{
			return m_depthStencilState;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			m_depthStencilState = value;
			value.IsLocked = true;
		}
	}

	public static BlendState BlendState
	{
		get
		{
			return m_blendState;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			m_blendState = value;
			value.IsLocked = true;
		}
	}

	public static RenderTarget2D RenderTarget
	{
		get
		{
			return m_renderTarget;
		}
		set
		{
			m_renderTarget = value;
			if (value != null)
			{
				Viewport = new Viewport(0, 0, value.Width, value.Height);
				ScissorRectangle = new Rectangle(0, 0, value.Width, value.Height);
			}
			else
			{
				Viewport = new Viewport(0, 0, BackbufferSize.X, BackbufferSize.Y);
				ScissorRectangle = new Rectangle(0, 0, BackbufferSize.X, BackbufferSize.Y);
			}
		}
	}

	public static string DeviceDescription { get; internal set; }

	public static event Action DeviceLost;

	public static event Action DeviceReset;

	public static void Clear(Color? color, float? depth = null, int? stencil = null)
	{
		Clear(color.HasValue ? new Vector4?(new Vector4(color.Value)) : ((Vector4?)null), depth, stencil);
	}

	internal static void VerifyParametersDrawUser<T>(PrimitiveType primitiveType, Shader shader, VertexDeclaration vertexDeclaration, T[] vertexData, int startVertex, int verticesCount) where T : struct
	{
		int num = Utilities.SizeOf<T>();
		if (shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		if (vertexDeclaration == null)
		{
			throw new ArgumentNullException("vertexDeclaration");
		}
		if (vertexData == null)
		{
			throw new ArgumentNullException("vertexData");
		}
		if (vertexDeclaration.VertexStride / num * num != vertexDeclaration.VertexStride)
		{
			throw new InvalidOperationException($"Vertex is not an integer multiple of array element, vertex stride is {vertexDeclaration.VertexStride}, array element is {num}.");
		}
		if (startVertex < 0 || verticesCount < 0 || startVertex + verticesCount > vertexData.Length)
		{
			throw new ArgumentException("Vertices range is out of bounds.");
		}
		shader.VerifyNotDisposed();
		m_statistics.UserDrawCallsCount++;
		m_statistics.UserRenderedPrimitivesCount += primitiveType.GetPrimitivesCount(verticesCount);
	}

	internal static void VerifyParametersDrawUserIndexed<T>(PrimitiveType primitiveType, Shader shader, VertexDeclaration vertexDeclaration, T[] vertexData, int startVertex, int verticesCount, ushort[] indexData, int startIndex, int indicesCount) where T : struct
	{
		int num = Utilities.SizeOf<T>();
		if (shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		if (vertexDeclaration == null)
		{
			throw new ArgumentNullException("vertexDeclaration");
		}
		if (vertexData == null)
		{
			throw new ArgumentNullException("vertexData");
		}
		if (indexData == null)
		{
			throw new ArgumentNullException("indexData");
		}
		if (vertexDeclaration.VertexStride / num * num != vertexDeclaration.VertexStride)
		{
			throw new InvalidOperationException($"Vertex is not an integer multiple of array element, vertex stride is {vertexDeclaration.VertexStride}, array element is {num}.");
		}
		if (startVertex < 0 || verticesCount < 0 || startVertex + verticesCount > vertexData.Length)
		{
			throw new ArgumentException("Vertices range is out of bounds.");
		}
		if (startIndex < 0 || indicesCount < 0 || startIndex + indicesCount > indexData.Length)
		{
			throw new ArgumentException("Indices range is out of bounds.");
		}
		shader.VerifyNotDisposed();
		m_statistics.UserDrawCallsCount++;
		m_statistics.UserRenderedPrimitivesCount += primitiveType.GetPrimitivesCount(indicesCount);
	}

	internal static void VerifyParametersDraw(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, int startVertex, int verticesCount)
	{
		if (shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		if (vertexBuffer == null)
		{
			throw new ArgumentNullException("vertexBuffer");
		}
		if (startVertex < 0 || verticesCount < 0 || startVertex + verticesCount > vertexBuffer.VerticesCount)
		{
			throw new ArgumentException("Vertices range is out of bounds.");
		}
		shader.VerifyNotDisposed();
		vertexBuffer.VerifyNotDisposed();
		m_statistics.UserDrawCallsCount++;
		m_statistics.UserRenderedPrimitivesCount += primitiveType.GetPrimitivesCount(verticesCount);
	}

	internal static void VerifyParametersDrawIndexed(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, IndexBuffer indexBuffer, int startIndex, int indicesCount)
	{
		if (shader == null)
		{
			throw new ArgumentNullException("shader");
		}
		if (vertexBuffer == null)
		{
			throw new ArgumentNullException("vertexBuffer");
		}
		if (indexBuffer == null)
		{
			throw new ArgumentNullException("indexBuffer");
		}
		if (startIndex < 0 || indicesCount < 0 || startIndex + indicesCount > indexBuffer.IndicesCount)
		{
			throw new ArgumentException("Indices range is out of bounds.");
		}
		shader.VerifyNotDisposed();
		vertexBuffer.VerifyNotDisposed();
		indexBuffer.VerifyNotDisposed();
		m_statistics.UserDrawCallsCount++;
		m_statistics.UserRenderedPrimitivesCount += primitiveType.GetPrimitivesCount(indicesCount);
	}

	internal static void HandleDeviceLost()
	{
		foreach (GraphicsResource resource in GraphicsResource.m_resources)
		{
			resource.HandleDeviceLost();
		}
		if (DeviceLost != null)
		{
			DeviceLost();
		}
	}

	internal static void HandleDeviceReset()
	{
		foreach (GraphicsResource resource in GraphicsResource.m_resources)
		{
			resource.HandleDeviceReset();
		}
		if (DeviceReset != null)
		{
			DeviceReset();
		}
	}

	public static void DrawUser<T>(PrimitiveType primitiveType, Shader shader, VertexDeclaration vertexDeclaration, T[] vertexData, int startVertex, int verticesCount) where T : struct
	{
		VerifyParametersDrawUser(primitiveType, shader, vertexDeclaration, vertexData, startVertex, verticesCount);
		GCHandle gCHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
		try
		{
			Wrapper.ApplyRenderTarget(RenderTarget);
			Wrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
			Wrapper.ApplyShaderAndBuffers(shader, vertexDeclaration, gCHandle.AddrOfPinnedObject() + startVertex * vertexDeclaration.VertexStride, 0, null);
			Wrapper.ApplyRasterizerState(RasterizerState);
			Wrapper.ApplyDepthStencilState(DepthStencilState);
			Wrapper.ApplyBlendState(BlendState);
			GL.DrawArrays(Wrapper.TranslatePrimitiveType(primitiveType), startVertex, verticesCount);
		}
		finally
		{
			gCHandle.Free();
		}
	}

	public static void DrawUserIndexed<T>(PrimitiveType primitiveType, Shader shader, VertexDeclaration vertexDeclaration, T[] vertexData, int startVertex, int verticesCount, ushort[] indexData, int startIndex, int indicesCount) where T : struct
	{
		VerifyParametersDrawUserIndexed(primitiveType, shader, vertexDeclaration, vertexData, startVertex, verticesCount, indexData, startIndex, indicesCount);
		GCHandle gCHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
		GCHandle gCHandle2 = GCHandle.Alloc(indexData, GCHandleType.Pinned);
		try
		{
			Wrapper.ApplyRenderTarget(RenderTarget);
			Wrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
			Wrapper.ApplyShaderAndBuffers(shader, vertexDeclaration, gCHandle.AddrOfPinnedObject(), 0, 0);
			Wrapper.ApplyRasterizerState(RasterizerState);
			Wrapper.ApplyDepthStencilState(DepthStencilState);
			Wrapper.ApplyBlendState(BlendState);
			GL.DrawElements(Wrapper.TranslatePrimitiveType(primitiveType), indicesCount, DrawElementsType.UnsignedShort, gCHandle2.AddrOfPinnedObject() + 2 * startIndex);
		}
		finally
		{
			gCHandle.Free();
			gCHandle2.Free();
		}
	}

	public static void Draw(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, int startVertex, int verticesCount)
	{
		VerifyParametersDraw(primitiveType, shader, vertexBuffer, startVertex, verticesCount);
		Wrapper.ApplyRenderTarget(RenderTarget);
		Wrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
		Wrapper.ApplyShaderAndBuffers(shader, vertexBuffer.VertexDeclaration, IntPtr.Zero, vertexBuffer.m_buffer, null);
		Wrapper.ApplyRasterizerState(RasterizerState);
		Wrapper.ApplyDepthStencilState(DepthStencilState);
		Wrapper.ApplyBlendState(BlendState);
		GL.DrawArrays(Wrapper.TranslatePrimitiveType(primitiveType), startVertex, verticesCount);
	}

	public static void DrawIndexed(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, IndexBuffer indexBuffer, int startIndex, int indicesCount)
	{
		VerifyParametersDrawIndexed(primitiveType, shader, vertexBuffer, indexBuffer, startIndex, indicesCount);
		Wrapper.ApplyRenderTarget(RenderTarget);
		Wrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
		Wrapper.ApplyShaderAndBuffers(shader, vertexBuffer.VertexDeclaration, IntPtr.Zero, vertexBuffer.m_buffer, indexBuffer.m_buffer);
		Wrapper.ApplyRasterizerState(RasterizerState);
		Wrapper.ApplyDepthStencilState(DepthStencilState);
		Wrapper.ApplyBlendState(BlendState);
		GL.DrawElements(Wrapper.TranslatePrimitiveType(primitiveType), indicesCount, Wrapper.TranslateIndexFormat(indexBuffer.IndexFormat), new IntPtr(startIndex * indexBuffer.IndexFormat.GetSize()));
	}

	public static void Clear(Vector4? color, float? depth = null, int? stencil = null)
	{
		Wrapper.Clear(RenderTarget, color, depth, stencil);
	}

	public static void ResetGLStateCache()
	{
		Wrapper.InitializeCache();
	}

	internal static void Initialize()
	{
		Wrapper.Initialize();
		Wrapper.InitializeCache();
		Resize();
	}

	internal static void Dispose()
	{
	}

	internal static void BeforeFrame()
	{
	}

	internal static void AfterFrame()
	{
	}

	internal static void Resize()
	{
		BackbufferSize = new Point2(Window.Size.X, Window.Size.Y);
		Viewport = new Viewport(0, 0, Window.Size.X, Window.Size.Y);
		ScissorRectangle = new Rectangle(0, 0, Window.Size.X, Window.Size.Y);
	}
}
