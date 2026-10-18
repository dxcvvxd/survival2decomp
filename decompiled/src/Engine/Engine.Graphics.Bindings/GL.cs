using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Engine.Graphics.Bindings;

[SuppressUnmanagedCodeSecurity]
internal static class GL
{
	private const string Library = "libGLESv2";

	[DllImport("libGLESv2", EntryPoint = "glGetError", ExactSpelling = true)]
	public static extern ErrorCode GetError();

	[DllImport("libGLESv2", EntryPoint = "glGetString", ExactSpelling = true)]
	private static extern nint GetStringCore(StringName name);

	public unsafe static string GetString(StringName name)
	{
		return new string((sbyte*)GetStringCore(name));
	}

	[DllImport("libGLESv2", EntryPoint = "glGetIntegerv", ExactSpelling = true)]
	public unsafe static extern void GetInteger(GetPName name, int* values);

	[DllImport("libGLESv2", EntryPoint = "glEnable", ExactSpelling = true)]
	public static extern void Enable(EnableCap cap);

	[DllImport("libGLESv2", EntryPoint = "glDisable", ExactSpelling = true)]
	public static extern void Disable(EnableCap cap);

	[DllImport("libGLESv2", EntryPoint = "glClear", ExactSpelling = true)]
	public static extern void Clear(ClearBufferMask mask);

	[DllImport("libGLESv2", EntryPoint = "glClearColor", ExactSpelling = true)]
	public static extern void ClearColor(float red, float green, float blue, float alpha);

	[DllImport("libGLESv2", EntryPoint = "glClearDepthf", ExactSpelling = true)]
	public static extern void ClearDepth(float depth);

	[DllImport("libGLESv2", EntryPoint = "glClearStencil", ExactSpelling = true)]
	public static extern void ClearStencil(int stencil);

	[DllImport("libGLESv2", EntryPoint = "glCullFace", ExactSpelling = true)]
	public static extern void CullFace(CullFaceMode mode);

	[DllImport("libGLESv2", EntryPoint = "glFrontFace", ExactSpelling = true)]
	public static extern void FrontFace(FrontFaceDirection mode);

	[DllImport("libGLESv2", EntryPoint = "glDepthFunc", ExactSpelling = true)]
	public static extern void DepthFunc(DepthFunction func);

	[DllImport("libGLESv2", EntryPoint = "glColorMask", ExactSpelling = true)]
	public static extern void ColorMask(bool red, bool green, bool blue, bool alpha);

	[DllImport("libGLESv2", EntryPoint = "glDepthMask", ExactSpelling = true)]
	public static extern void DepthMask(bool depth);

	[DllImport("libGLESv2", EntryPoint = "glPolygonOffset", ExactSpelling = true)]
	public static extern void PolygonOffset(float factor, float units);

	[DllImport("libGLESv2", EntryPoint = "glBlendColor", ExactSpelling = true)]
	public static extern void BlendColor(float red, float green, float blue, float alpha);

	[DllImport("libGLESv2", EntryPoint = "glBlendEquation", ExactSpelling = true)]
	public static extern void BlendEquation(BlendEquationMode mode);

	[DllImport("libGLESv2", EntryPoint = "glBlendEquationSeparate", ExactSpelling = true)]
	public static extern void BlendEquationSeparate(BlendEquationMode modeColor, BlendEquationMode modeAlpha);

	[DllImport("libGLESv2", EntryPoint = "glBlendFunc", ExactSpelling = true)]
	public static extern void BlendFunc(BlendingFactor srcFactor, BlendingFactor dstFactor);

	[DllImport("libGLESv2", EntryPoint = "glBlendFuncSeparate", ExactSpelling = true)]
	public static extern void BlendFuncSeparate(BlendingFactorSrc srcFactorColor, BlendingFactorDest dstFactorColor, BlendingFactorSrc srcFactorAlpha, BlendingFactorDest dstFactorAlpha);

	[DllImport("libGLESv2", EntryPoint = "glEnableVertexAttribArray", ExactSpelling = true)]
	public static extern void EnableVertexAttribArray(int index);

	[DllImport("libGLESv2", EntryPoint = "glDisableVertexAttribArray", ExactSpelling = true)]
	public static extern void DisableVertexAttribArray(int index);

	[DllImport("libGLESv2", EntryPoint = "glBindTexture", ExactSpelling = true)]
	public static extern void BindTexture(TextureTarget target, int texture);

	[DllImport("libGLESv2", EntryPoint = "glActiveTexture", ExactSpelling = true)]
	public static extern void ActiveTexture(TextureUnit unit);

	[DllImport("libGLESv2", EntryPoint = "glBindBuffer", ExactSpelling = true)]
	public static extern void BindBuffer(BufferTarget target, int log);

	[DllImport("libGLESv2", EntryPoint = "glBindFramebuffer", ExactSpelling = true)]
	public static extern void BindFramebuffer(FramebufferTarget target, int framebuffer);

	[DllImport("libGLESv2", EntryPoint = "glBindRenderbuffer", ExactSpelling = true)]
	public static extern void BindRenderbuffer(RenderbufferTarget target, int renderbuffer);

	[DllImport("libGLESv2", EntryPoint = "glUseProgram", ExactSpelling = true)]
	public static extern void UseProgram(int program);

	[DllImport("libGLESv2", EntryPoint = "glDeleteProgram", ExactSpelling = true)]
	public static extern void DeleteProgram(int program);

	[DllImport("libGLESv2", EntryPoint = "glGenTextures", ExactSpelling = true)]
	public unsafe static extern void GenTextures(int n, int* textures);

	[DllImport("libGLESv2", EntryPoint = "glDeleteTextures", ExactSpelling = true)]
	public unsafe static extern void DeleteTextures(int n, int* textures);

	[DllImport("libGLESv2", EntryPoint = "glGenFramebuffers", ExactSpelling = true)]
	public unsafe static extern void GenFramebuffers(int n, int* framebuffers);

	[DllImport("libGLESv2", EntryPoint = "glDeleteFramebuffers", ExactSpelling = true)]
	public unsafe static extern void DeleteFramebuffers(int n, int* framebuffers);

	[DllImport("libGLESv2", EntryPoint = "glGenBuffers", ExactSpelling = true)]
	public unsafe static extern void GenBuffers(int n, int* buffers);

	[DllImport("libGLESv2", EntryPoint = "glDeleteBuffers", ExactSpelling = true)]
	public unsafe static extern void DeleteBuffers(int n, int* buffers);

	[DllImport("libGLESv2", EntryPoint = "glGenRenderbuffers", ExactSpelling = true)]
	public unsafe static extern void GenRenderbuffers(int n, int* renderbuffers);

	[DllImport("libGLESv2", EntryPoint = "glDeleteRenderbuffers", ExactSpelling = true)]
	public unsafe static extern void DeleteRenderbuffers(int n, int* renderbuffers);

	[DllImport("libGLESv2", EntryPoint = "glViewport", ExactSpelling = true)]
	public static extern void Viewport(int x, int y, int width, int height);

	[DllImport("libGLESv2", EntryPoint = "glDepthRangef", ExactSpelling = true)]
	public static extern void DepthRange(float zNear, float zFar);

	[DllImport("libGLESv2", EntryPoint = "glScissor", ExactSpelling = true)]
	public static extern void Scissor(int x, int y, int width, int height);

	[DllImport("libGLESv2", EntryPoint = "glVertexAttribPointer", ExactSpelling = true)]
	public static extern void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, nint pointer);

	[DllImport("libGLESv2", EntryPoint = "glUniform1i", ExactSpelling = true)]
	public static extern void Uniform1(int location, int value);

	[DllImport("libGLESv2", EntryPoint = "glUniform1fv", ExactSpelling = true)]
	public unsafe static extern void Uniform1(int location, int count, float* values);

	[DllImport("libGLESv2", EntryPoint = "glUniform2fv", ExactSpelling = true)]
	public unsafe static extern void Uniform2(int location, int count, float* values);

	[DllImport("libGLESv2", EntryPoint = "glUniform3fv", ExactSpelling = true)]
	public unsafe static extern void Uniform3(int location, int count, float* values);

	[DllImport("libGLESv2", EntryPoint = "glUniform4fv", ExactSpelling = true)]
	public unsafe static extern void Uniform4(int location, int count, float* values);

	[DllImport("libGLESv2", EntryPoint = "glUniformMatrix4fv", ExactSpelling = true)]
	public unsafe static extern void UniformMatrix4(int location, int count, bool transpose, float* values);

	[DllImport("libGLESv2", EntryPoint = "glTexParameteri", ExactSpelling = true)]
	public static extern void TexParameter(TextureTarget target, TextureParameterName parameter, int value);

	[DllImport("libGLESv2", EntryPoint = "glTexParameterf", ExactSpelling = true)]
	public static extern void TexParameter(TextureTarget target, TextureParameterName parameter, float value);

	[DllImport("libGLESv2", EntryPoint = "glDrawArrays", ExactSpelling = true)]
	public static extern void DrawArrays(PrimitiveType mode, int first, int count);

	[DllImport("libGLESv2", EntryPoint = "glDrawElements", ExactSpelling = true)]
	public static extern void DrawElements(PrimitiveType mode, int count, DrawElementsType type, nint indices);

	[DllImport("libGLESv2", EntryPoint = "glBufferSubData", ExactSpelling = true)]
	public static extern void BufferSubData(BufferTarget target, nint offset, nint size, nint data);

	[DllImport("libGLESv2", EntryPoint = "glBufferData", ExactSpelling = true)]
	internal static extern void BufferData(BufferTarget target, nint size, nint data, BufferUsageHint usage);

	[DllImport("libGLESv2", EntryPoint = "glTexImage2D", ExactSpelling = true)]
	public static extern void TexImage2D(TextureTarget target, int level, PixelInternalFormat internalformat, int width, int height, int border, PixelFormat format, PixelType type, nint pixels);

	[DllImport("libGLESv2", EntryPoint = "glGenerateMipmap", ExactSpelling = true)]
	public static extern void GenerateMipmap(TextureTarget target);

	[DllImport("libGLESv2", EntryPoint = "glFramebufferTexture2D", ExactSpelling = true)]
	public static extern void FramebufferTexture2D(FramebufferTarget target, FramebufferSlot attachment, TextureTarget textureTarget, int texture, int level);

	[DllImport("libGLESv2", EntryPoint = "glRenderbufferStorage", ExactSpelling = true)]
	public static extern void RenderbufferStorage(RenderbufferTarget target, RenderbufferStorage internalformat, int width, int height);

	[DllImport("libGLESv2", EntryPoint = "glFramebufferRenderbuffer", ExactSpelling = true)]
	public static extern void FramebufferRenderbuffer(FramebufferTarget target, FramebufferSlot attachment, RenderbufferTarget renderbuffertarget, int renderbuffer);

	[DllImport("libGLESv2", EntryPoint = "glCheckFramebufferStatus", ExactSpelling = true)]
	public static extern FramebufferErrorCode CheckFramebufferStatus(FramebufferTarget target);

	[DllImport("libGLESv2", EntryPoint = "glReadPixels", ExactSpelling = true)]
	public static extern void ReadPixels(int x, int y, int width, int height, PixelFormat format, PixelType type, nint pixels);

	[DllImport("libGLESv2", EntryPoint = "glCreateShader", ExactSpelling = true)]
	public static extern int CreateShader(ShaderType type);

	[DllImport("libGLESv2", EntryPoint = "glDeleteShader", ExactSpelling = true)]
	public static extern void DeleteShader(int shader);

	[DllImport("libGLESv2", EntryPoint = "glShaderSource", ExactSpelling = true)]
	public unsafe static extern void ShaderSource(int shader, int count, string[] strings, int* lengths);

	[DllImport("libGLESv2", EntryPoint = "glCompileShader", ExactSpelling = true)]
	public static extern void CompileShader(int shader);

	[DllImport("libGLESv2", EntryPoint = "glGetShaderiv", ExactSpelling = true)]
	public unsafe static extern void GetShader(int shader, ShaderParameterName name, int* values);

	[DllImport("libGLESv2", EntryPoint = "glGetShaderInfoLog", ExactSpelling = true)]
	private unsafe static extern void GetShaderInfoLog(int shader, int bufferSize, int* length, StringBuilder log);

	public unsafe static void GetShaderInfoLog(int shader, int bufferSize, int* length, out string log)
	{
		StringBuilder stringBuilder = new StringBuilder(bufferSize + 1);
		GetShaderInfoLog(shader, bufferSize, length, stringBuilder);
		log = stringBuilder.ToString();
	}

	[DllImport("libGLESv2", EntryPoint = "glCreateProgram", ExactSpelling = true)]
	public static extern int CreateProgram();

	[DllImport("libGLESv2", EntryPoint = "glAttachShader", ExactSpelling = true)]
	public static extern void AttachShader(int program, int shader);

	[DllImport("libGLESv2", EntryPoint = "glDetachShader", ExactSpelling = true)]
	public static extern void DetachShader(int program, int shader);

	[DllImport("libGLESv2", EntryPoint = "glLinkProgram", ExactSpelling = true)]
	public static extern void LinkProgram(int program);

	[DllImport("libGLESv2", EntryPoint = "glGetProgramiv", ExactSpelling = true)]
	public unsafe static extern void GetProgram(int program, ProgramParameterName name, int* values);

	[DllImport("libGLESv2", EntryPoint = "glGetProgramInfoLog", ExactSpelling = true)]
	private unsafe static extern void GetProgramInfoLog(int program, int bufferSize, int* length, StringBuilder log);

	public unsafe static void GetProgramInfoLog(int program, int bufferSize, int* length, out string log)
	{
		StringBuilder stringBuilder = new StringBuilder(bufferSize + 1);
		GetProgramInfoLog(program, bufferSize, length, stringBuilder);
		log = stringBuilder.ToString();
	}

	[DllImport("libGLESv2", EntryPoint = "glGetActiveAttrib", ExactSpelling = true)]
	private unsafe static extern void GetActiveAttrib(int program, int index, int bufferSize, int* length, int* size, ActiveAttribType* type, StringBuilder name);

	public unsafe static void GetActiveAttrib(int program, int index, int bufferSize, int* length, int* size, ActiveAttribType* type, out string name)
	{
		StringBuilder stringBuilder = new StringBuilder(bufferSize + 1);
		GetActiveAttrib(program, index, bufferSize, length, size, type, stringBuilder);
		name = stringBuilder.ToString();
	}

	[DllImport("libGLESv2", EntryPoint = "glGetAttribLocation", ExactSpelling = true)]
	public static extern int GetAttribLocation(int program, string name);

	[DllImport("libGLESv2", EntryPoint = "glGetActiveUniform", ExactSpelling = true)]
	private unsafe static extern void GetActiveUniform(int program, int index, int bufferSize, int* length, int* size, ActiveUniformType* type, StringBuilder name);

	public unsafe static void GetActiveUniform(int program, int index, int bufferSize, int* length, int* size, ActiveUniformType* type, out string name)
	{
		StringBuilder stringBuilder = new StringBuilder(bufferSize + 1);
		GetActiveUniform(program, index, bufferSize, length, size, type, stringBuilder);
		name = stringBuilder.ToString();
	}

	[DllImport("libGLESv2", EntryPoint = "glGetUniformLocation", ExactSpelling = true)]
	public static extern int GetUniformLocation(int program, string name);

	[DllImport("libGLESv2", EntryPoint = "glGetRenderbufferParameteriv", ExactSpelling = true)]
	public unsafe static extern void GetRenderbufferParameter(RenderbufferTarget target, RenderbufferParameterName name, int* values);
}
