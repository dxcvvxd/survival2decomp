using System;
using Engine;

namespace Game;

public class TerrainGeometry : IDisposable
{
	public TerrainGeometrySubset SubsetOpaque;

	public TerrainGeometrySubset SubsetAlphaTest;

	public TerrainGeometrySubset SubsetTransparent;

	public TerrainGeometrySubset[] OpaqueSubsetsByFace;

	public TerrainGeometrySubset[] AlphaTestSubsetsByFace;

	public TerrainGeometrySubset[] TransparentSubsetsByFace;

	public void Dispose()
	{
		Utilities.Dispose(ref SubsetOpaque);
		Utilities.Dispose(ref SubsetAlphaTest);
		Utilities.Dispose(ref SubsetTransparent);
		for (int i = 0; i < OpaqueSubsetsByFace.Length; i++)
		{
			Utilities.Dispose(ref OpaqueSubsetsByFace[i]);
		}
		for (int j = 0; j < AlphaTestSubsetsByFace.Length; j++)
		{
			Utilities.Dispose(ref AlphaTestSubsetsByFace[j]);
		}
		for (int k = 0; k < TransparentSubsetsByFace.Length; k++)
		{
			Utilities.Dispose(ref TransparentSubsetsByFace[k]);
		}
	}
}
