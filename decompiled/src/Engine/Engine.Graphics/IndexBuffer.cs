using System;
using System.Runtime.InteropServices;
using Engine.Graphics.Bindings;

namespace Engine.Graphics;

public sealed class IndexBuffer : GraphicsResource
{
	internal int m_buffer;

	public IndexFormat IndexFormat { get; private set; }

	public int IndicesCount { get; private set; }

	public object Tag { get; set; }

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

	public override int GetGpuMemoryUsage()
	{
		return IndicesCount * IndexFormat.GetSize();
	}

	private void InitializeIndexBuffer(IndexFormat indexFormat, int indicesCount)
	{
		if (indicesCount <= 0)
		{
			throw new ArgumentException("Indices count must be greater than 0.");
		}
		IndexFormat = indexFormat;
		IndicesCount = indicesCount;
	}

	private void VerifyParametersSetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct
	{
		VerifyNotDisposed();
		int num = Utilities.SizeOf<T>();
		int size = IndexFormat.GetSize();
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (sourceStartIndex < 0 || sourceCount < 0 || sourceStartIndex + sourceCount > source.Length)
		{
			throw new ArgumentException("Range is out of source bounds.");
		}
		if (targetStartIndex < 0 || targetStartIndex * size + sourceCount * num > IndicesCount * size)
		{
			throw new ArgumentException("Range is out of target bounds.");
		}
	}

	public IndexBuffer(IndexFormat indexFormat, int indicesCount)
	{
		InitializeIndexBuffer(indexFormat, indicesCount);
		AllocateBuffer();
	}

	public override void Dispose()
	{
		base.Dispose();
		DeleteBuffer();
	}

	public void SetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : struct
	{
		VerifyParametersSetData(source, sourceStartIndex, sourceCount, targetStartIndex);
		GCHandle gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
		try
		{
			int num = Utilities.SizeOf<T>();
			int size = IndexFormat.GetSize();
			Wrapper.BindBuffer(BufferTarget.ElementArrayBuffer, m_buffer);
			GL.BufferSubData(BufferTarget.ElementArrayBuffer, new IntPtr(targetStartIndex * size), new IntPtr(num * sourceCount), gCHandle.AddrOfPinnedObject() + sourceStartIndex * num);
		}
		finally
		{
			gCHandle.Free();
		}
	}

	internal override void HandleDeviceLost()
	{
		DeleteBuffer();
	}

	internal override void HandleDeviceReset()
	{
		AllocateBuffer();
	}

	private unsafe void AllocateBuffer()
	{
		int buffer = 0;
		GL.GenBuffers(1, &buffer);
		m_buffer = buffer;
		Wrapper.BindBuffer(BufferTarget.ElementArrayBuffer, m_buffer);
		GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(IndexFormat.GetSize() * IndicesCount), IntPtr.Zero, BufferUsageHint.StaticDraw);
	}

	private void DeleteBuffer()
	{
		if (m_buffer != 0)
		{
			Wrapper.DeleteBuffer(BufferTarget.ElementArrayBuffer, m_buffer);
			m_buffer = 0;
		}
	}
}
