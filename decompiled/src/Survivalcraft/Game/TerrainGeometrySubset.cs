using System;
using Engine;

namespace Game;

public class TerrainGeometrySubset : IDisposable
{
	public TerrainGeometryDynamicArray<TerrainVertex> Vertices = new TerrainGeometryDynamicArray<TerrainVertex>();

	public TerrainGeometryDynamicArray<ushort> Indices = new TerrainGeometryDynamicArray<ushort>();

	public void Dispose()
	{
		Utilities.Dispose(ref Vertices);
		Utilities.Dispose(ref Indices);
	}
}
