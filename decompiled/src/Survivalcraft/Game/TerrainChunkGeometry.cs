using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class TerrainChunkGeometry : IDisposable
{
	public class Buffer : IDisposable
	{
		public VertexBuffer VertexBuffer;

		public IndexBuffer IndexBuffer;

		public int[] SubsetIndexBufferStarts = new int[7];

		public int[] SubsetIndexBufferEnds = new int[7];

		public void Dispose()
		{
			Utilities.Dispose(ref VertexBuffer);
			Utilities.Dispose(ref IndexBuffer);
		}
	}

	public const int SubsetsCount = 7;

	public TerrainChunkSliceGeometry[] Slices = new TerrainChunkSliceGeometry[16];

	public DynamicArray<Buffer> Buffers = new DynamicArray<Buffer>();

	public TerrainChunkGeometry()
	{
		for (int i = 0; i < Slices.Length; i++)
		{
			Slices[i] = new TerrainChunkSliceGeometry();
		}
	}

	public void Dispose()
	{
		DisposeVertexIndexBuffers();
		TerrainChunkSliceGeometry[] slices = Slices;
		for (int i = 0; i < slices.Length; i++)
		{
			slices[i].Dispose();
		}
	}

	public void InvalidateSlicesGeometryHashes()
	{
		TerrainChunkSliceGeometry[] slices = Slices;
		for (int i = 0; i < slices.Length; i++)
		{
			slices[i].GeometryHash = 0;
		}
	}

	public void DisposeVertexIndexBuffers()
	{
		foreach (Buffer buffer in Buffers)
		{
			buffer.Dispose();
		}
		Buffers.Clear();
	}
}
