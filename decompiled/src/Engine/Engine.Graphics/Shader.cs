using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine.Graphics.Bindings;

namespace Engine.Graphics;

public class Shader : GraphicsResource
{
	internal struct ShaderAttributeData
	{
		public string Semantic;

		public int Location;
	}

	internal struct VertexAttributeData
	{
		public int Size;

		public VertexAttribPointerType Type;

		public bool Normalize;

		public int Offset;
	}

	private Dictionary<string, ShaderParameter> m_parametersByName;

	internal ShaderParameter[] m_parameters;

	private string m_vertexShaderCode;

	private string m_pixelShaderCode;

	private ShaderMacro[] m_shaderMacros;

	internal int m_program;

	internal int m_vertexShader;

	internal int m_pixelShader;

	internal Dictionary<VertexDeclaration, VertexAttributeData[]> m_vertexAttributeDataByDeclaration = new Dictionary<VertexDeclaration, VertexAttributeData[]>();

	internal List<ShaderAttributeData> m_shaderAttributeData = new List<ShaderAttributeData>();

	private ShaderParameter m_glymulParameter;

	public object Tag { get; set; }

	public ReadOnlyList<ShaderParameter> Parameters => new ReadOnlyList<ShaderParameter>(m_parameters);

	public string DebugName
	{
		get
		{
			return string.Empty;
		}
		set
		{
		}
	}

	public ShaderParameter GetParameter(string name, bool allowNull = false)
	{
		if (!m_parametersByName.TryGetValue(name, out var value) && !allowNull)
		{
			throw new InvalidOperationException($"Parameter \"{name}\" not found.");
		}
		return value;
	}

	public override int GetGpuMemoryUsage()
	{
		return 16384;
	}

	protected virtual void PrepareForDrawingOverride()
	{
	}

	private void InitializeShader(string vertexShaderCode, string pixelShaderCode, ShaderMacro[] shaderMacros)
	{
		if (vertexShaderCode == null)
		{
			throw new ArgumentNullException("vertexShaderCode");
		}
		if (pixelShaderCode == null)
		{
			throw new ArgumentNullException("pixelShaderCode");
		}
		if (shaderMacros == null)
		{
			throw new ArgumentNullException("shaderMacros");
		}
		m_vertexShaderCode = vertexShaderCode;
		m_pixelShaderCode = pixelShaderCode;
		m_shaderMacros = (ShaderMacro[])shaderMacros.Clone();
	}

	public Shader(string vertexShaderCode, string pixelShaderCode, params ShaderMacro[] shaderMacros)
	{
		try
		{
			InitializeShader(vertexShaderCode, pixelShaderCode, shaderMacros);
			CompileShaders();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		DeleteShaders();
	}

	internal void PrepareForDrawing()
	{
		m_glymulParameter.SetValue((Display.RenderTarget != null) ? (-1f) : 1f);
		PrepareForDrawingOverride();
	}

	internal VertexAttributeData[] GetVertexAttribData(VertexDeclaration vertexDeclaration)
	{
		if (!m_vertexAttributeDataByDeclaration.TryGetValue(vertexDeclaration, out var value))
		{
			value = new VertexAttributeData[8];
			foreach (ShaderAttributeData shaderAttributeDatum in m_shaderAttributeData)
			{
				VertexElement vertexElement = null;
				for (int i = 0; i < vertexDeclaration.m_elements.Length; i++)
				{
					if (vertexDeclaration.m_elements[i].Semantic == shaderAttributeDatum.Semantic)
					{
						vertexElement = vertexDeclaration.m_elements[i];
						break;
					}
				}
				if (vertexElement != null)
				{
					value[shaderAttributeDatum.Location] = new VertexAttributeData
					{
						Size = vertexElement.Format.GetElementsCount(),
						Offset = vertexElement.Offset
					};
					Wrapper.TranslateVertexElementFormat(vertexElement.Format, out value[shaderAttributeDatum.Location].Type, out value[shaderAttributeDatum.Location].Normalize);
					continue;
				}
				throw new InvalidOperationException($"VertexElement not found for shader attribute \"{shaderAttributeDatum.Semantic}\".");
			}
			m_vertexAttributeDataByDeclaration.Add(vertexDeclaration, value);
		}
		return value;
	}

	private static void ParseShaderMetadata(string shaderCode, Dictionary<string, string> semanticsByAttribute, Dictionary<string, string> samplersByTexture)
	{
		string[] array = shaderCode.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				string text = array[i];
				text = text.Trim();
				if (!text.StartsWith("//"))
				{
					continue;
				}
				text = text.Substring(2).TrimStart();
				if (!text.StartsWith("<") || !text.EndsWith("/>"))
				{
					continue;
				}
				XElement xElement = XElement.Parse(text);
				if (xElement.Name == "Semantic")
				{
					if (xElement.Attribute("Attribute") == null)
					{
						throw new InvalidOperationException("Missing \"Attribute\" attribute in shader metadata.");
					}
					if (xElement.Attribute("Name") == null)
					{
						throw new InvalidOperationException("Missing \"Name\" attribute in shader metadata.");
					}
					semanticsByAttribute.Add(xElement.Attribute("Attribute").Value, xElement.Attribute("Name").Value);
					continue;
				}
				if (xElement.Name == "Sampler")
				{
					if (xElement.Attribute("Texture") == null)
					{
						throw new InvalidOperationException("Missing \"Texture\" attribute in shader metadata.");
					}
					if (xElement.Attribute("Name") == null)
					{
						throw new InvalidOperationException("Missing \"Name\" attribute in shader metadata.");
					}
					string value = xElement.Attribute("Texture").Value;
					string value2 = xElement.Attribute("Name").Value;
					if (samplersByTexture.TryGetValue(value, out var value3))
					{
						if (value3 != value2)
						{
							throw new InvalidOperationException($"Two different sampler definitions for texture \"{value}\", it is already defined to \"{value2}\".");
						}
					}
					else
					{
						samplersByTexture.Add(value, value2);
					}
					continue;
				}
				throw new InvalidOperationException("Unrecognized shader metadata node.");
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Error in shader metadata, line {i + 1}. {ex.Message}");
			}
		}
	}

	private string PrependShaderMacros(string shaderCode, ShaderMacro[] shaderMacros, bool isVertexShader)
	{
		string text = "#version 100" + Environment.NewLine;
		text = text + "#define GLSL" + Environment.NewLine;
		if (isVertexShader)
		{
			text = text + "#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul; gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;" + Environment.NewLine;
			text = text + "uniform float u_glymul;" + Environment.NewLine;
		}
		foreach (ShaderMacro shaderMacro in shaderMacros)
		{
			text = text + "#define " + shaderMacro.Name + " " + shaderMacro.Value + Environment.NewLine;
		}
		text = text + "#line 1" + Environment.NewLine;
		return text + shaderCode;
	}

	internal override void HandleDeviceLost()
	{
		DeleteShaders();
	}

	internal override void HandleDeviceReset()
	{
		CompileShaders();
	}

	internal unsafe void CompileShaders()
	{
		DeleteShaders();
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		ParseShaderMetadata(m_vertexShaderCode, dictionary, dictionary2);
		ParseShaderMetadata(m_pixelShaderCode, dictionary, dictionary2);
		string text = PrependShaderMacros(m_vertexShaderCode, m_shaderMacros, isVertexShader: true);
		string text2 = PrependShaderMacros(m_pixelShaderCode, m_shaderMacros, isVertexShader: false);
		m_vertexShader = GL.CreateShader(ShaderType.VertexShader);
		GL.ShaderSource(m_vertexShader, 1, new string[1] { text }, null);
		GL.CompileShader(m_vertexShader);
		int num = 0;
		GL.GetShader(m_vertexShader, ShaderParameterName.CompileStatus, &num);
		if (num != 1)
		{
			string shaderInfoLog = GetShaderInfoLog(m_vertexShader);
			throw new InvalidOperationException($"Error compiling vertex shader.\n{shaderInfoLog}");
		}
		m_pixelShader = GL.CreateShader(ShaderType.FragmentShader);
		GL.ShaderSource(m_pixelShader, 1, new string[1] { text2 }, null);
		GL.CompileShader(m_pixelShader);
		int num2 = 0;
		GL.GetShader(m_pixelShader, ShaderParameterName.CompileStatus, &num2);
		if (num2 != 1)
		{
			string shaderInfoLog2 = GetShaderInfoLog(m_pixelShader);
			throw new InvalidOperationException($"Error compiling pixel shader.\n{shaderInfoLog2}");
		}
		m_program = GL.CreateProgram();
		GL.AttachShader(m_program, m_vertexShader);
		GL.AttachShader(m_program, m_pixelShader);
		GL.LinkProgram(m_program);
		int num3 = 0;
		GL.GetProgram(m_program, ProgramParameterName.LinkStatus, &num3);
		if (num3 != 1)
		{
			string programInfoLog = GetProgramInfoLog(m_program);
			throw new InvalidOperationException($"Error linking program.\n{programInfoLog}");
		}
		int num4 = 0;
		GL.GetProgram(m_program, ProgramParameterName.ActiveAttributes, &num4);
		for (int i = 0; i < num4; i++)
		{
			int num5 = 0;
			int num6 = 0;
			ActiveAttribType activeAttribType = (ActiveAttribType)0;
			GL.GetActiveAttrib(m_program, i, 256, &num5, &num6, &activeAttribType, out var name);
			int attribLocation = GL.GetAttribLocation(m_program, name);
			if (!dictionary.TryGetValue(name, out var value))
			{
				throw new InvalidOperationException($"Attribute \"{name}\" has no semantic defined in shader metadata.");
			}
			m_shaderAttributeData.Add(new ShaderAttributeData
			{
				Location = attribLocation,
				Semantic = value
			});
		}
		int num7 = 0;
		GL.GetProgram(m_program, ProgramParameterName.ActiveUniforms, &num7);
		List<ShaderParameter> list = new List<ShaderParameter>();
		Dictionary<string, ShaderParameter> dictionary3 = new Dictionary<string, ShaderParameter>();
		for (int j = 0; j < num7; j++)
		{
			int num8 = 0;
			int count = 0;
			ActiveUniformType type = (ActiveUniformType)0;
			GL.GetActiveUniform(m_program, j, 256, &num8, &count, &type, out var name2);
			int uniformLocation = GL.GetUniformLocation(m_program, name2);
			ShaderParameterType shaderParameterType = Wrapper.TranslateActiveUniformType(type);
			int num9 = name2.IndexOf('[');
			if (num9 >= 0)
			{
				name2 = name2.Remove(num9, name2.Length - num9);
			}
			ShaderParameter shaderParameter = new ShaderParameter(this, name2, shaderParameterType, count);
			shaderParameter.Location = uniformLocation;
			dictionary3.Add(shaderParameter.Name, shaderParameter);
			list.Add(shaderParameter);
			if (shaderParameterType == ShaderParameterType.Texture2D)
			{
				if (!dictionary2.TryGetValue(shaderParameter.Name, out var value2))
				{
					throw new InvalidOperationException($"Texture \"{shaderParameter.Name}\" has no sampler defined in shader metadata.");
				}
				ShaderParameter shaderParameter2 = new ShaderParameter(this, value2, ShaderParameterType.Sampler2D, 1);
				shaderParameter2.Location = int.MaxValue;
				dictionary3.Add(value2, shaderParameter2);
				list.Add(shaderParameter2);
			}
		}
		if (m_parameters != null)
		{
			foreach (KeyValuePair<string, ShaderParameter> item in dictionary3)
			{
				if (m_parametersByName.TryGetValue(item.Key, out var value3))
				{
					value3.Location = item.Value.Location;
				}
			}
			ShaderParameter[] parameters = m_parameters;
			for (int k = 0; k < parameters.Length; k++)
			{
				parameters[k].IsChanged = true;
			}
		}
		else
		{
			m_parameters = list.ToArray();
			m_parametersByName = dictionary3;
		}
		m_glymulParameter = GetParameter("u_glymul");
		if (m_glymulParameter.Type != ShaderParameterType.Float)
		{
			throw new InvalidOperationException("u_glymul parameter has invalid type.");
		}
	}

	internal void DeleteShaders()
	{
		if (m_program != 0)
		{
			if (m_vertexShader != 0)
			{
				GL.DetachShader(m_program, m_vertexShader);
			}
			if (m_pixelShader != 0)
			{
				GL.DetachShader(m_program, m_pixelShader);
			}
			Wrapper.DeleteProgram(m_program);
			m_program = 0;
		}
		if (m_vertexShader != 0)
		{
			GL.DeleteShader(m_vertexShader);
			m_vertexShader = 0;
		}
		if (m_pixelShader != 0)
		{
			GL.DeleteShader(m_pixelShader);
			m_pixelShader = 0;
		}
	}

	private unsafe static string GetShaderInfoLog(int shader)
	{
		int num = 0;
		GL.GetShader(shader, ShaderParameterName.InfoLogLength, &num);
		if (num == 0)
		{
			return string.Empty;
		}
		GL.GetShaderInfoLog(shader, num, &num, out var log);
		return log;
	}

	private unsafe static string GetProgramInfoLog(int program)
	{
		int num = 0;
		GL.GetProgram(program, ProgramParameterName.InfoLogLength, &num);
		if (num == 0)
		{
			return string.Empty;
		}
		GL.GetProgramInfoLog(program, num, &num, out var log);
		return log;
	}
}
