using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Graphics.Bindings;

namespace Engine.Graphics;

internal static class Wrapper
{
	internal static int m_mainFramebuffer;

	internal static int m_mainColorbuffer;

	private static int m_arrayBuffer;

	private static int m_elementArrayBuffer;

	private static int m_texture2D;

	private static int[] m_activeTexturesByUnit;

	private static TextureUnit m_activeTextureUnit;

	private static int m_program;

	private static int m_framebuffer;

	private static Vector4? m_clearColor;

	private static float? m_clearDepth;

	private static int? m_clearStencil;

	private static CullFaceMode m_cullFace;

	private static FrontFaceDirection m_frontFace;

	private static DepthFunction m_depthFunction;

	private static int? m_colorMask;

	private static bool? m_depthMask;

	private static float m_polygonOffsetFactor;

	private static float m_polygonOffsetUnits;

	private static Vector4 m_blendColor;

	private static BlendEquationMode m_blendEquation;

	private static BlendEquationMode m_blendEquationColor;

	private static BlendEquationMode m_blendEquationAlpha;

	private static BlendingFactor m_blendFactorSource;

	private static BlendingFactorSrc m_blendFuncSourceColor;

	private static BlendingFactorSrc m_blendFuncSourceAlpha;

	private static BlendingFactor m_blendFactorDestination;

	private static BlendingFactorDest m_blendFuncDestinationColor;

	private static BlendingFactorDest m_blendFuncDestinationAlpha;

	private static Dictionary<EnableCap, bool> m_enableDisableStates;

	private static bool?[] m_vertexAttribArray;

	private static RasterizerState m_rasterizerState;

	private static DepthStencilState m_depthStencilState;

	private static BlendState m_blendState;

	private static Dictionary<int, SamplerState> m_textureSamplerStates;

	private static Shader m_lastShader;

	private static VertexDeclaration m_lastVertexDeclaration;

	private static nint m_lastVertexOffset;

	private static int m_lastArrayBuffer;

	private static Viewport? m_viewport;

	private static Rectangle? m_scissorRectangle;

	private static bool GL_EXT_texture_filter_anisotropic;

	private static bool GL_OES_packed_depth_stencil;

	public static void Initialize()
	{
		Display.DeviceDescription = $"OpenGL, Vendor={GL.GetString(StringName.Vendor) ?? string.Empty}, Renderer={GL.GetString(StringName.Renderer) ?? string.Empty}, Version={GL.GetString(StringName.Version) ?? string.Empty}";
		Log.Information("Initialized display device: " + Display.DeviceDescription);
		string text = GL.GetString(StringName.Extensions);
		GL_EXT_texture_filter_anisotropic = text.Contains("GL_EXT_texture_filter_anisotropic");
		GL_OES_packed_depth_stencil = text.Contains("GL_OES_packed_depth_stencil");
	}

	public static void InitializeCache()
	{
		m_arrayBuffer = -1;
		m_elementArrayBuffer = -1;
		m_texture2D = -1;
		m_activeTexturesByUnit = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
		m_activeTextureUnit = (TextureUnit)(-1);
		m_program = -1;
		m_framebuffer = -1;
		m_clearColor = null;
		m_clearDepth = null;
		m_clearStencil = null;
		m_cullFace = (CullFaceMode)0;
		m_frontFace = (FrontFaceDirection)0;
		m_depthFunction = (DepthFunction)(-1);
		m_colorMask = null;
		m_depthMask = null;
		m_polygonOffsetFactor = 0f;
		m_polygonOffsetUnits = 0f;
		m_blendColor = new Vector4(float.MinValue);
		m_blendEquation = (BlendEquationMode)(-1);
		m_blendEquationColor = (BlendEquationMode)(-1);
		m_blendEquationAlpha = (BlendEquationMode)(-1);
		m_blendFactorSource = (BlendingFactor)(-1);
		m_blendFuncSourceColor = (BlendingFactorSrc)(-1);
		m_blendFuncSourceAlpha = (BlendingFactorSrc)(-1);
		m_blendFactorDestination = (BlendingFactor)(-1);
		m_blendFuncDestinationColor = (BlendingFactorDest)(-1);
		m_blendFuncDestinationAlpha = (BlendingFactorDest)(-1);
		m_enableDisableStates = new Dictionary<EnableCap, bool>();
		m_vertexAttribArray = new bool?[16];
		m_rasterizerState = null;
		m_depthStencilState = null;
		m_blendState = null;
		m_textureSamplerStates = new Dictionary<int, SamplerState>();
		m_lastShader = null;
		m_lastVertexDeclaration = null;
		m_lastVertexOffset = IntPtr.Zero;
		m_lastArrayBuffer = -1;
		m_viewport = null;
		m_scissorRectangle = null;
	}

	public unsafe static void LogFramebufferParameters()
	{
		int* ptr = stackalloc int[4];
		int num = default(int);
		GL.GetInteger(GetPName.RedBits, &num);
		int num2 = default(int);
		GL.GetInteger(GetPName.GreenBits, &num2);
		int num3 = default(int);
		GL.GetInteger(GetPName.BlueBits, &num3);
		int num4 = default(int);
		GL.GetInteger(GetPName.AlphaBits, &num4);
		int num5 = default(int);
		GL.GetInteger(GetPName.DepthBits, &num5);
		int num6 = default(int);
		GL.GetInteger(GetPName.StencilBits, &num6);
		GL.GetInteger(GetPName.Viewport, ptr);
		Log.Information("OpenGL framebuffer {0}x{1}, R={2} G={3} B={4} A={5}, D={6} S={7}", ptr[2], ptr[3], num, num2, num3, num4, num5, num6);
	}

	public static bool Enable(EnableCap state)
	{
		if (!m_enableDisableStates.TryGetValue(state, out var value) || !value)
		{
			GL.Enable(state);
			m_enableDisableStates[state] = true;
			return true;
		}
		return false;
	}

	public static bool Disable(EnableCap state)
	{
		if (!m_enableDisableStates.TryGetValue(state, out var value) | value)
		{
			GL.Disable(state);
			m_enableDisableStates[state] = false;
			return true;
		}
		return false;
	}

	public static void ClearColor(Vector4 color)
	{
		Vector4 value = color;
		Vector4? clearColor = m_clearColor;
		if (value != clearColor)
		{
			GL.ClearColor(color.X, color.Y, color.Z, color.W);
			m_clearColor = color;
		}
	}

	public static void ClearDepth(float depth)
	{
		if (depth != m_clearDepth)
		{
			GL.ClearDepth(depth);
			m_clearDepth = depth;
		}
	}

	public static void ClearStencil(int stencil)
	{
		if (stencil != m_clearStencil)
		{
			GL.ClearStencil(stencil);
			m_clearStencil = stencil;
		}
	}

	public static void CullFace(CullFaceMode cullFace)
	{
		if (cullFace != m_cullFace)
		{
			GL.CullFace(cullFace);
			m_cullFace = cullFace;
		}
	}

	public static void FrontFace(FrontFaceDirection frontFace)
	{
		if (frontFace != m_frontFace)
		{
			GL.FrontFace(frontFace);
			m_frontFace = frontFace;
		}
	}

	public static void DepthFunc(DepthFunction depthFunction)
	{
		if (depthFunction != m_depthFunction)
		{
			GL.DepthFunc(depthFunction);
			m_depthFunction = depthFunction;
		}
	}

	public static void ColorMask(int colorMask)
	{
		colorMask &= 0xF;
		if (colorMask != m_colorMask)
		{
			GL.ColorMask((colorMask & 8) != 0, (colorMask & 4) != 0, (colorMask & 2) != 0, (colorMask & 1) != 0);
			m_colorMask = colorMask;
		}
	}

	public static bool DepthMask(bool depthMask)
	{
		if (depthMask != m_depthMask)
		{
			GL.DepthMask(depthMask);
			m_depthMask = depthMask;
			return true;
		}
		return false;
	}

	public static void PolygonOffset(float factor, float units)
	{
		if (factor != m_polygonOffsetFactor || units != m_polygonOffsetUnits)
		{
			GL.PolygonOffset(factor, units);
			m_polygonOffsetFactor = factor;
			m_polygonOffsetUnits = units;
		}
	}

	public static void BlendColor(Vector4 blendColor)
	{
		if (blendColor != m_blendColor)
		{
			GL.BlendColor(blendColor.X, blendColor.Y, blendColor.Z, blendColor.W);
			m_blendColor = blendColor;
		}
	}

	public static void BlendEquation(BlendEquationMode blendEquation)
	{
		if (blendEquation != m_blendEquation)
		{
			GL.BlendEquation(blendEquation);
			m_blendEquation = blendEquation;
			m_blendEquationColor = (BlendEquationMode)(-1);
			m_blendEquationAlpha = (BlendEquationMode)(-1);
		}
	}

	public static void BlendEquationSeparate(BlendEquationMode blendEquationColor, BlendEquationMode blendEquationAlpha)
	{
		if (blendEquationColor != m_blendEquationColor || blendEquationAlpha != m_blendEquationAlpha)
		{
			GL.BlendEquationSeparate(blendEquationColor, blendEquationAlpha);
			m_blendEquationColor = blendEquationColor;
			m_blendEquationAlpha = blendEquationAlpha;
			m_blendEquation = (BlendEquationMode)(-1);
		}
	}

	public static void BlendFunc(BlendingFactor blendFactorSource, BlendingFactor blendFactorDestination)
	{
		if (blendFactorSource != m_blendFactorSource || blendFactorDestination != m_blendFactorDestination)
		{
			GL.BlendFunc(blendFactorSource, blendFactorDestination);
			m_blendFactorSource = blendFactorSource;
			m_blendFactorDestination = blendFactorDestination;
			m_blendFuncSourceColor = (BlendingFactorSrc)(-1);
			m_blendFuncSourceAlpha = (BlendingFactorSrc)(-1);
			m_blendFuncDestinationColor = (BlendingFactorDest)(-1);
			m_blendFuncDestinationAlpha = (BlendingFactorDest)(-1);
		}
	}

	public static void BlendFuncSeparate(BlendingFactorSrc blendFactorSourceColor, BlendingFactorDest blendFactorDestinationColor, BlendingFactorSrc blendFactorSourceAlpha, BlendingFactorDest blendFactorDestinationAlpha)
	{
		if (blendFactorSourceColor != m_blendFuncSourceColor || blendFactorDestinationColor != m_blendFuncDestinationColor || blendFactorSourceAlpha != m_blendFuncSourceAlpha || blendFactorDestinationAlpha != m_blendFuncDestinationAlpha)
		{
			GL.BlendFuncSeparate(blendFactorSourceColor, blendFactorDestinationColor, blendFactorSourceAlpha, blendFactorDestinationAlpha);
			m_blendFuncSourceColor = blendFactorSourceColor;
			m_blendFuncSourceAlpha = blendFactorSourceAlpha;
			m_blendFuncDestinationColor = blendFactorDestinationColor;
			m_blendFuncDestinationAlpha = blendFactorDestinationAlpha;
			m_blendFactorSource = (BlendingFactor)(-1);
			m_blendFactorDestination = (BlendingFactor)(-1);
		}
	}

	public static void VertexAttribArray(int index, bool enable)
	{
		if (enable && (!m_vertexAttribArray[index].HasValue || !m_vertexAttribArray[index].Value))
		{
			GL.EnableVertexAttribArray(index);
			m_vertexAttribArray[index] = true;
		}
		else if (!enable && (!m_vertexAttribArray[index].HasValue || m_vertexAttribArray[index].Value))
		{
			GL.DisableVertexAttribArray(index);
			m_vertexAttribArray[index] = false;
		}
	}

	public static void BindTexture(TextureTarget target, int texture, bool forceBind)
	{
		if (target == TextureTarget.Texture2D)
		{
			if (forceBind || texture != m_texture2D)
			{
				GL.BindTexture(target, texture);
				m_texture2D = texture;
				if (m_activeTextureUnit >= (TextureUnit)0)
				{
					m_activeTexturesByUnit[(int)(m_activeTextureUnit - 33984)] = texture;
				}
			}
		}
		else
		{
			GL.BindTexture(target, texture);
		}
	}

	public static void ActiveTexture(TextureUnit textureUnit)
	{
		if (textureUnit != m_activeTextureUnit)
		{
			GL.ActiveTexture(textureUnit);
			m_activeTextureUnit = textureUnit;
		}
	}

	public static void BindBuffer(BufferTarget target, int buffer)
	{
		switch (target)
		{
		case BufferTarget.ArrayBuffer:
			if (buffer != m_arrayBuffer)
			{
				GL.BindBuffer(target, buffer);
				m_arrayBuffer = buffer;
			}
			break;
		case BufferTarget.ElementArrayBuffer:
			if (buffer != m_elementArrayBuffer)
			{
				GL.BindBuffer(target, buffer);
				m_elementArrayBuffer = buffer;
			}
			break;
		default:
			GL.BindBuffer(target, buffer);
			break;
		}
	}

	public static void BindFramebuffer(int framebuffer)
	{
		if (framebuffer != m_framebuffer)
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
			m_framebuffer = framebuffer;
		}
	}

	public static void UseProgram(int program)
	{
		if (program != m_program)
		{
			GL.UseProgram(program);
			m_program = program;
		}
	}

	public static void DeleteProgram(int program)
	{
		if (m_program == program)
		{
			m_program = -1;
		}
		GL.DeleteProgram(program);
	}

	public unsafe static void DeleteTexture(int texture)
	{
		if (m_texture2D == texture)
		{
			m_texture2D = -1;
		}
		for (int i = 0; i < m_activeTexturesByUnit.Length; i++)
		{
			if (m_activeTexturesByUnit[i] == texture)
			{
				m_activeTexturesByUnit[i] = -1;
			}
		}
		m_textureSamplerStates.Remove(texture);
		GL.DeleteTextures(1, &texture);
	}

	public unsafe static void DeleteFramebuffer(int framebuffer)
	{
		if (m_framebuffer == framebuffer)
		{
			m_framebuffer = -1;
		}
		GL.DeleteFramebuffers(1, &framebuffer);
	}

	public unsafe static void DeleteBuffer(BufferTarget target, int buffer)
	{
		if (target == BufferTarget.ArrayBuffer)
		{
			if (m_arrayBuffer == buffer)
			{
				m_arrayBuffer = -1;
			}
			if (m_lastArrayBuffer == buffer)
			{
				m_lastArrayBuffer = -1;
			}
		}
		if (target == BufferTarget.ElementArrayBuffer && m_elementArrayBuffer == buffer)
		{
			m_elementArrayBuffer = -1;
		}
		GL.DeleteBuffers(1, &buffer);
	}

	public static void ApplyViewportScissor(Viewport viewport, Rectangle scissorRectangle, bool isScissorEnabled)
	{
		if (!m_viewport.HasValue || viewport.X != m_viewport.Value.X || viewport.Y != m_viewport.Value.Y || viewport.Width != m_viewport.Value.Width || viewport.Height != m_viewport.Value.Height)
		{
			int y = ((Display.RenderTarget == null) ? (Display.BackbufferSize.Y - viewport.Y - viewport.Height) : viewport.Y);
			GL.Viewport(viewport.X, y, viewport.Width, viewport.Height);
		}
		if (!m_viewport.HasValue || viewport.MinDepth != m_viewport.Value.MinDepth || viewport.MaxDepth != m_viewport.Value.MaxDepth)
		{
			GL.DepthRange(viewport.MinDepth, viewport.MaxDepth);
		}
		m_viewport = viewport;
		if (!isScissorEnabled)
		{
			return;
		}
		if (m_scissorRectangle.HasValue)
		{
			Rectangle value = scissorRectangle;
			Rectangle? scissorRectangle2 = m_scissorRectangle;
			if (!(value != scissorRectangle2))
			{
				return;
			}
		}
		if (Display.RenderTarget == null)
		{
			scissorRectangle.Top = Display.BackbufferSize.Y - scissorRectangle.Top - scissorRectangle.Height;
		}
		GL.Scissor(scissorRectangle.Left, scissorRectangle.Top, scissorRectangle.Width, scissorRectangle.Height);
		m_scissorRectangle = scissorRectangle;
	}

	public static void ApplyRasterizerState(RasterizerState state)
	{
		if (state != m_rasterizerState)
		{
			m_rasterizerState = state;
			switch (state.CullMode)
			{
			case CullMode.None:
				Disable(EnableCap.CullFace);
				break;
			case CullMode.CullClockwise:
				Enable(EnableCap.CullFace);
				CullFace(CullFaceMode.Back);
				FrontFace((Display.RenderTarget != null) ? FrontFaceDirection.Cw : FrontFaceDirection.Ccw);
				break;
			case CullMode.CullCounterClockwise:
				Enable(EnableCap.CullFace);
				CullFace(CullFaceMode.Back);
				FrontFace((Display.RenderTarget != null) ? FrontFaceDirection.Ccw : FrontFaceDirection.Cw);
				break;
			}
			if (state.ScissorTestEnable)
			{
				Enable(EnableCap.ScissorTest);
			}
			else
			{
				Disable(EnableCap.ScissorTest);
			}
			if (state.DepthBias != 0f || state.SlopeScaleDepthBias != 0f)
			{
				Enable(EnableCap.PolygonOffsetFill);
				PolygonOffset(state.SlopeScaleDepthBias, state.DepthBias);
			}
			else
			{
				Disable(EnableCap.PolygonOffsetFill);
			}
		}
	}

	public static void ApplyDepthStencilState(DepthStencilState state)
	{
		if (state == m_depthStencilState)
		{
			return;
		}
		m_depthStencilState = state;
		if (state.DepthBufferTestEnable || state.DepthBufferWriteEnable)
		{
			Enable(EnableCap.DepthTest);
			if (state.DepthBufferTestEnable)
			{
				DepthFunc(TranslateCompareFunction(state.DepthBufferFunction));
			}
			else
			{
				DepthFunc(DepthFunction.Always);
			}
			DepthMask(state.DepthBufferWriteEnable);
		}
		else
		{
			Disable(EnableCap.DepthTest);
		}
	}

	public static void ApplyBlendState(BlendState state)
	{
		if (state == m_blendState)
		{
			return;
		}
		m_blendState = state;
		if (state.ColorBlendFunction == BlendFunction.Add && state.ColorSourceBlend == Blend.One && state.ColorDestinationBlend == Blend.Zero && state.AlphaBlendFunction == BlendFunction.Add && state.AlphaSourceBlend == Blend.One && state.AlphaDestinationBlend == Blend.Zero)
		{
			Disable(EnableCap.Blend);
			return;
		}
		BlendEquationMode blendEquationMode = TranslateBlendFunction(state.ColorBlendFunction);
		BlendEquationMode blendEquationMode2 = TranslateBlendFunction(state.AlphaBlendFunction);
		BlendingFactor blendingFactor = TranslateBlend(state.ColorSourceBlend);
		BlendingFactor blendingFactor2 = TranslateBlend(state.ColorDestinationBlend);
		BlendingFactor blendingFactor3 = TranslateBlend(state.AlphaSourceBlend);
		BlendingFactor blendingFactor4 = TranslateBlend(state.AlphaDestinationBlend);
		if (blendEquationMode == blendEquationMode2 && blendingFactor == blendingFactor3 && blendingFactor2 == blendingFactor4)
		{
			BlendEquation(blendEquationMode);
			BlendFunc(blendingFactor, blendingFactor2);
		}
		else
		{
			BlendEquationSeparate(blendEquationMode, blendEquationMode2);
			BlendFuncSeparate((BlendingFactorSrc)blendingFactor, (BlendingFactorDest)blendingFactor2, (BlendingFactorSrc)blendingFactor3, (BlendingFactorDest)blendingFactor4);
		}
		BlendColor(state.BlendFactor);
		Enable(EnableCap.Blend);
	}

	public static void ApplyRenderTarget(RenderTarget2D renderTarget)
	{
		if (renderTarget != null)
		{
			BindFramebuffer(renderTarget.m_frameBuffer);
		}
		else
		{
			BindFramebuffer(m_mainFramebuffer);
		}
	}

	public unsafe static void ApplyShaderAndBuffers(Shader shader, VertexDeclaration vertexDeclaration, nint vertexOffset, int arrayBuffer, int? elementArrayBuffer)
	{
		shader.PrepareForDrawing();
		BindBuffer(BufferTarget.ArrayBuffer, arrayBuffer);
		if (elementArrayBuffer.HasValue)
		{
			BindBuffer(BufferTarget.ElementArrayBuffer, elementArrayBuffer.Value);
		}
		UseProgram(shader.m_program);
		if (shader != m_lastShader || vertexOffset != m_lastVertexOffset || arrayBuffer != m_lastArrayBuffer || vertexDeclaration.m_elements != m_lastVertexDeclaration.m_elements)
		{
			Shader.VertexAttributeData[] vertexAttribData = shader.GetVertexAttribData(vertexDeclaration);
			for (int i = 0; i < vertexAttribData.Length; i++)
			{
				if (vertexAttribData[i].Size != 0)
				{
					GL.VertexAttribPointer(i, vertexAttribData[i].Size, vertexAttribData[i].Type, vertexAttribData[i].Normalize, vertexDeclaration.VertexStride, vertexOffset + vertexAttribData[i].Offset);
					VertexAttribArray(i, enable: true);
				}
				else
				{
					VertexAttribArray(i, enable: false);
				}
			}
			m_lastShader = shader;
			m_lastVertexDeclaration = vertexDeclaration;
			m_lastVertexOffset = vertexOffset;
			m_lastArrayBuffer = arrayBuffer;
		}
		int num = 0;
		for (int j = 0; j < shader.m_parameters.Length; j++)
		{
			ShaderParameter shaderParameter = shader.m_parameters[j];
			if (shaderParameter.IsChanged)
			{
				switch (shaderParameter.Type)
				{
				case ShaderParameterType.Float:
					fixed (float* value2 = shaderParameter.Value)
					{
						GL.Uniform1(shaderParameter.Location, shaderParameter.Count, value2);
					}
					shaderParameter.IsChanged = false;
					break;
				case ShaderParameterType.Vector2:
					fixed (float* value4 = shaderParameter.Value)
					{
						GL.Uniform2(shaderParameter.Location, shaderParameter.Count, value4);
					}
					shaderParameter.IsChanged = false;
					break;
				case ShaderParameterType.Vector3:
					fixed (float* value5 = shaderParameter.Value)
					{
						GL.Uniform3(shaderParameter.Location, shaderParameter.Count, value5);
					}
					shaderParameter.IsChanged = false;
					break;
				case ShaderParameterType.Vector4:
					fixed (float* value3 = shaderParameter.Value)
					{
						GL.Uniform4(shaderParameter.Location, shaderParameter.Count, value3);
					}
					shaderParameter.IsChanged = false;
					break;
				case ShaderParameterType.Matrix:
					fixed (float* value = shaderParameter.Value)
					{
						GL.UniformMatrix4(shaderParameter.Location, shaderParameter.Count, transpose: false, value);
					}
					shaderParameter.IsChanged = false;
					break;
				default:
					throw new InvalidOperationException("Unsupported shader parameter type.");
				case ShaderParameterType.Texture2D:
				case ShaderParameterType.Sampler2D:
					break;
				}
			}
			if (shaderParameter.Type != ShaderParameterType.Texture2D)
			{
				continue;
			}
			if (num >= 8)
			{
				throw new InvalidOperationException("Too many simultaneous textures.");
			}
			ActiveTexture((TextureUnit)(33984 + num));
			if (shaderParameter.IsChanged)
			{
				GL.Uniform1(shaderParameter.Location, num);
			}
			ShaderParameter obj = shader.m_parameters[j + 1];
			Texture2D texture2D = (Texture2D)shaderParameter.Resource;
			SamplerState samplerState = (SamplerState)obj.Resource;
			if (texture2D != null)
			{
				if (samplerState == null)
				{
					throw new InvalidOperationException($"Associated SamplerState is not set for texture \"{shaderParameter.Name}\".");
				}
				if (m_activeTexturesByUnit[num] != texture2D.m_texture)
				{
					BindTexture(TextureTarget.Texture2D, texture2D.m_texture, forceBind: true);
				}
				if (!m_textureSamplerStates.TryGetValue(texture2D.m_texture, out var value6) || value6 != samplerState)
				{
					BindTexture(TextureTarget.Texture2D, texture2D.m_texture, forceBind: false);
					if (GL_EXT_texture_filter_anisotropic)
					{
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropyExt, (samplerState.FilterMode == TextureFilterMode.Anisotropic) ? ((float)samplerState.MaxAnisotropy) : 1f);
					}
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TranslateTextureFilterModeMin(samplerState.FilterMode, texture2D.MipLevelsCount > 1));
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TranslateTextureFilterModeMag(samplerState.FilterMode));
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TranslateTextureAddressMode(samplerState.AddressModeU));
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TranslateTextureAddressMode(samplerState.AddressModeV));
					m_textureSamplerStates[texture2D.m_texture] = samplerState;
				}
			}
			else if (m_activeTexturesByUnit[num] != 0)
			{
				BindTexture(TextureTarget.Texture2D, 0, forceBind: true);
			}
			num++;
			shaderParameter.IsChanged = false;
		}
	}

	public static void Clear(RenderTarget2D renderTarget, Vector4? color, float? depth, int? stencil)
	{
		ClearBufferMask clearBufferMask = (ClearBufferMask)0;
		if (color.HasValue)
		{
			clearBufferMask |= ClearBufferMask.ColorBufferBit;
			ClearColor(color.Value);
			ColorMask(15);
		}
		if (depth.HasValue)
		{
			clearBufferMask |= ClearBufferMask.DepthBufferBit;
			ClearDepth(depth.Value);
			if (DepthMask(depthMask: true))
			{
				m_depthStencilState = null;
			}
		}
		if (stencil.HasValue)
		{
			clearBufferMask |= ClearBufferMask.StencilBufferBit;
			ClearStencil(stencil.Value);
		}
		if (clearBufferMask != 0)
		{
			ApplyRenderTarget(renderTarget);
			if (Disable(EnableCap.ScissorTest))
			{
				m_rasterizerState = null;
			}
			GL.Clear(clearBufferMask);
		}
	}

	public static void HandleContextLost()
	{
		try
		{
			Log.Information("Device lost");
			Display.HandleDeviceLost();
			GC.Collect();
			InitializeCache();
			Display.Resize();
			Display.HandleDeviceReset();
			Log.Information("Device reset");
		}
		catch (Exception ex)
		{
			Log.Error("Failed to recreate graphics resources. Reason: {0}", ex.Message);
		}
	}

	public static void TranslateVertexElementFormat(VertexElementFormat vertexElementFormat, out VertexAttribPointerType type, out bool normalize)
	{
		switch (vertexElementFormat)
		{
		case VertexElementFormat.Single:
			type = VertexAttribPointerType.Float;
			normalize = false;
			break;
		case VertexElementFormat.Vector2:
			type = VertexAttribPointerType.Float;
			normalize = false;
			break;
		case VertexElementFormat.Vector3:
			type = VertexAttribPointerType.Float;
			normalize = false;
			break;
		case VertexElementFormat.Vector4:
			type = VertexAttribPointerType.Float;
			normalize = false;
			break;
		case VertexElementFormat.Byte4:
			type = VertexAttribPointerType.UnsignedByte;
			normalize = false;
			break;
		case VertexElementFormat.NormalizedByte4:
			type = VertexAttribPointerType.UnsignedByte;
			normalize = true;
			break;
		case VertexElementFormat.Short2:
			type = VertexAttribPointerType.Short;
			normalize = false;
			break;
		case VertexElementFormat.NormalizedShort2:
			type = VertexAttribPointerType.Short;
			normalize = true;
			break;
		case VertexElementFormat.Short4:
			type = VertexAttribPointerType.Short;
			normalize = false;
			break;
		case VertexElementFormat.NormalizedShort4:
			type = VertexAttribPointerType.Short;
			normalize = true;
			break;
		default:
			throw new InvalidOperationException("Unsupported vertex element format.");
		}
	}

	public static DrawElementsType TranslateIndexFormat(IndexFormat indexFormat)
	{
		if (indexFormat == IndexFormat.SixteenBits)
		{
			return DrawElementsType.UnsignedShort;
		}
		throw new InvalidOperationException("Unsupported index format.");
	}

	public static ShaderParameterType TranslateActiveUniformType(ActiveUniformType type)
	{
		return type switch
		{
			ActiveUniformType.Float => ShaderParameterType.Float, 
			ActiveUniformType.FloatVec2 => ShaderParameterType.Vector2, 
			ActiveUniformType.FloatVec3 => ShaderParameterType.Vector3, 
			ActiveUniformType.FloatVec4 => ShaderParameterType.Vector4, 
			ActiveUniformType.FloatMat4 => ShaderParameterType.Matrix, 
			ActiveUniformType.Sampler2D => ShaderParameterType.Texture2D, 
			_ => throw new InvalidOperationException("Unsupported shader parameter type."), 
		};
	}

	public static Engine.Graphics.Bindings.PrimitiveType TranslatePrimitiveType(PrimitiveType primitiveType)
	{
		return primitiveType switch
		{
			PrimitiveType.LineList => Engine.Graphics.Bindings.PrimitiveType.Lines, 
			PrimitiveType.LineStrip => Engine.Graphics.Bindings.PrimitiveType.LineStrip, 
			PrimitiveType.TriangleList => Engine.Graphics.Bindings.PrimitiveType.Triangles, 
			PrimitiveType.TriangleStrip => Engine.Graphics.Bindings.PrimitiveType.TriangleStrip, 
			_ => throw new InvalidOperationException("Unsupported primitive type."), 
		};
	}

	public static TextureMinFilter TranslateTextureFilterModeMin(TextureFilterMode filterMode, bool isMipmapped)
	{
		switch (filterMode)
		{
		case TextureFilterMode.Point:
			if (!isMipmapped)
			{
				return TextureMinFilter.Nearest;
			}
			return TextureMinFilter.NearestMipmapNearest;
		case TextureFilterMode.Linear:
			if (!isMipmapped)
			{
				return TextureMinFilter.Linear;
			}
			return TextureMinFilter.LinearMipmapLinear;
		case TextureFilterMode.Anisotropic:
			if (!isMipmapped)
			{
				return TextureMinFilter.Linear;
			}
			return TextureMinFilter.LinearMipmapLinear;
		case TextureFilterMode.PointMipLinear:
			if (!isMipmapped)
			{
				return TextureMinFilter.Nearest;
			}
			return TextureMinFilter.NearestMipmapLinear;
		case TextureFilterMode.LinearMipPoint:
			if (!isMipmapped)
			{
				return TextureMinFilter.Linear;
			}
			return TextureMinFilter.LinearMipmapNearest;
		case TextureFilterMode.MinPointMagLinearMipPoint:
			if (!isMipmapped)
			{
				return TextureMinFilter.Nearest;
			}
			return TextureMinFilter.NearestMipmapNearest;
		case TextureFilterMode.MinPointMagLinearMipLinear:
			if (!isMipmapped)
			{
				return TextureMinFilter.Nearest;
			}
			return TextureMinFilter.NearestMipmapLinear;
		case TextureFilterMode.MinLinearMagPointMipPoint:
			if (!isMipmapped)
			{
				return TextureMinFilter.Linear;
			}
			return TextureMinFilter.LinearMipmapNearest;
		case TextureFilterMode.MinLinearMagPointMipLinear:
			if (!isMipmapped)
			{
				return TextureMinFilter.Linear;
			}
			return TextureMinFilter.LinearMipmapLinear;
		default:
			throw new InvalidOperationException("Unsupported texture filter mode.");
		}
	}

	public static TextureMagFilter TranslateTextureFilterModeMag(TextureFilterMode filterMode)
	{
		return filterMode switch
		{
			TextureFilterMode.Point => TextureMagFilter.Nearest, 
			TextureFilterMode.Linear => TextureMagFilter.Linear, 
			TextureFilterMode.Anisotropic => TextureMagFilter.Linear, 
			TextureFilterMode.PointMipLinear => TextureMagFilter.Nearest, 
			TextureFilterMode.LinearMipPoint => TextureMagFilter.Nearest, 
			TextureFilterMode.MinPointMagLinearMipPoint => TextureMagFilter.Linear, 
			TextureFilterMode.MinPointMagLinearMipLinear => TextureMagFilter.Linear, 
			TextureFilterMode.MinLinearMagPointMipPoint => TextureMagFilter.Nearest, 
			TextureFilterMode.MinLinearMagPointMipLinear => TextureMagFilter.Nearest, 
			_ => throw new InvalidOperationException("Unsupported texture filter mode."), 
		};
	}

	public static TextureWrapMode TranslateTextureAddressMode(TextureAddressMode addressMode)
	{
		return addressMode switch
		{
			TextureAddressMode.Clamp => TextureWrapMode.ClampToEdge, 
			TextureAddressMode.Wrap => TextureWrapMode.Repeat, 
			TextureAddressMode.MirrorWrap => TextureWrapMode.MirroredRepeat, 
			_ => throw new InvalidOperationException("Unsupported texture address mode."), 
		};
	}

	public static DepthFunction TranslateCompareFunction(CompareFunction compareFunction)
	{
		return compareFunction switch
		{
			CompareFunction.Always => DepthFunction.Always, 
			CompareFunction.Equal => DepthFunction.Equal, 
			CompareFunction.Greater => DepthFunction.Greater, 
			CompareFunction.GreaterEqual => DepthFunction.Gequal, 
			CompareFunction.Less => DepthFunction.Less, 
			CompareFunction.LessEqual => DepthFunction.Lequal, 
			CompareFunction.Never => DepthFunction.Never, 
			CompareFunction.NotEqual => DepthFunction.Notequal, 
			_ => throw new InvalidOperationException("Unsupported compare function."), 
		};
	}

	public static BlendEquationMode TranslateBlendFunction(BlendFunction blendFunction)
	{
		return blendFunction switch
		{
			BlendFunction.Add => BlendEquationMode.FuncAdd, 
			BlendFunction.Subtract => BlendEquationMode.FuncSubtract, 
			BlendFunction.ReverseSubtract => BlendEquationMode.FuncReverseSubtract, 
			_ => throw new InvalidOperationException("Unsupported blend function."), 
		};
	}

	public static BlendingFactor TranslateBlend(Blend blend)
	{
		return blend switch
		{
			Blend.Zero => BlendingFactor.Zero, 
			Blend.One => BlendingFactor.One, 
			Blend.SourceColor => BlendingFactor.SrcColor, 
			Blend.InverseSourceColor => BlendingFactor.OneMinusSrcColor, 
			Blend.DestinationColor => BlendingFactor.DstColor, 
			Blend.InverseDestinationColor => BlendingFactor.OneMinusDstColor, 
			Blend.SourceAlpha => BlendingFactor.SrcAlpha, 
			Blend.InverseSourceAlpha => BlendingFactor.OneMinusSrcAlpha, 
			Blend.DestinationAlpha => BlendingFactor.DstAlpha, 
			Blend.InverseDestinationAlpha => BlendingFactor.OneMinusDstAlpha, 
			Blend.BlendFactor => BlendingFactor.ConstantColor, 
			Blend.InverseBlendFactor => BlendingFactor.OneMinusConstantColor, 
			Blend.SourceAlphaSaturation => BlendingFactor.SrcAlphaSaturate, 
			_ => throw new InvalidOperationException("Unsupported blend."), 
		};
	}

	public static RenderbufferStorage TranslateDepthFormat(DepthFormat depthFormat)
	{
		return depthFormat switch
		{
			DepthFormat.Depth16 => RenderbufferStorage.DepthComponent16, 
			DepthFormat.Depth24Stencil8 => RenderbufferStorage.Depth24Stencil8, 
			_ => throw new InvalidOperationException("Unsupported DepthFormat."), 
		};
	}

	[Conditional("DEBUG")]
	public static void CheckGLError(string func)
	{
	}
}
