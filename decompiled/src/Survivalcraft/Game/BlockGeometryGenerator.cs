using System;
using System.Runtime.CompilerServices;
using Engine;

namespace Game;

public class BlockGeometryGenerator
{
	private struct CornerLights
	{
		public float L000;

		public float L001;

		public float L010;

		public float L011;

		public float L100;

		public float L101;

		public float L110;

		public float L111;
	}

	private static Vector2[] m_textureCoordinates = new Vector2[8]
	{
		new Vector2(0.001f, 0.999f),
		new Vector2(0.999f, 0.999f),
		new Vector2(0.999f, 0.001f),
		new Vector2(0.001f, 0.001f),
		new Vector2(0.001f, 0.999f),
		new Vector2(0.999f, 0.999f),
		new Vector2(0.999f, 0.001f),
		new Vector2(0.001f, 0.001f)
	};

	public readonly Terrain Terrain;

	public readonly SubsystemTerrain SubsystemTerrain;

	public readonly SubsystemElectricity SubsystemElectricity;

	public readonly SubsystemFurnitureBlockBehavior SubsystemFurnitureBlockBehavior;

	public readonly SubsystemMetersBlockBehavior SubsystemMetersBlockBehavior;

	public readonly SubsystemPalette SubsystemPalette;

	private DynamicArray<ElectricConnectionPath> m_tmpConnectionPaths = new DynamicArray<ElectricConnectionPath>();

	private Point3 m_cornerLightsPosition;

	private CornerLights[] m_cornerLightsByFace = new CornerLights[6];

	private bool[] m_visibleSides = new bool[6];

	public BlockGeometryGenerator(Terrain terrain, SubsystemTerrain subsystemTerrain, SubsystemElectricity subsystemElectricity, SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior, SubsystemMetersBlockBehavior subsystemMetersBlockBehavior, SubsystemPalette subsystemPalette)
	{
		Terrain = terrain;
		SubsystemTerrain = subsystemTerrain;
		SubsystemElectricity = subsystemElectricity;
		SubsystemFurnitureBlockBehavior = subsystemFurnitureBlockBehavior;
		SubsystemMetersBlockBehavior = subsystemMetersBlockBehavior;
		SubsystemPalette = subsystemPalette;
		ResetCache();
	}

	public void ResetCache()
	{
		m_cornerLightsPosition = new Point3(int.MaxValue);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetupLitCornerVertex(float x, float y, float z, Color color, int textureSlot, int corner, ref TerrainVertex vertex)
	{
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % 16)) / 16f;
		float ty = (m_textureCoordinates[corner].Y + (float)(textureSlot / 16)) / 16f;
		SetupVertex(x, y, z, color, tx, ty, ref vertex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetupVertex(float x, float y, float z, Color color, float tx, float ty, ref TerrainVertex vertex)
	{
		vertex.X = x;
		vertex.Y = y;
		vertex.Z = z;
		vertex.Tx = (short)(tx * 32767f);
		vertex.Ty = (short)(ty * 32767f);
		vertex.Color = color;
	}

	public void GenerateCrossfaceVertices(Block block, int value, int x, int y, int z, Color color, int textureSlot, TerrainGeometrySubset subset)
	{
		DynamicArray<TerrainVertex> vertices = subset.Vertices;
		TerrainGeometryDynamicArray<ushort> indices = subset.Indices;
		int num = Terrain.ExtractLight(value);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 64];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		int count = vertices.Count;
		vertices.Count += 8;
		if ((x & 1) == 0)
		{
			SetupLitCornerVertex(x, y, z, color2, textureSlot, 0, ref vertices.Array[count]);
			SetupLitCornerVertex(x + 1, y, z + 1, color2, textureSlot, 1, ref vertices.Array[count + 1]);
			SetupLitCornerVertex(x + 1, y + 1, z + 1, color2, textureSlot, 2, ref vertices.Array[count + 2]);
			SetupLitCornerVertex(x, y + 1, z, color2, textureSlot, 3, ref vertices.Array[count + 3]);
		}
		else
		{
			SetupLitCornerVertex(x, y, z, color2, textureSlot, 1, ref vertices.Array[count]);
			SetupLitCornerVertex(x + 1, y, z + 1, color2, textureSlot, 0, ref vertices.Array[count + 1]);
			SetupLitCornerVertex(x + 1, y + 1, z + 1, color2, textureSlot, 3, ref vertices.Array[count + 2]);
			SetupLitCornerVertex(x, y + 1, z, color2, textureSlot, 2, ref vertices.Array[count + 3]);
		}
		if ((z & 1) == 0)
		{
			SetupLitCornerVertex(x, y, z + 1, color2, textureSlot, 0, ref vertices.Array[count + 4]);
			SetupLitCornerVertex(x + 1, y, z, color2, textureSlot, 1, ref vertices.Array[count + 5]);
			SetupLitCornerVertex(x + 1, y + 1, z, color2, textureSlot, 2, ref vertices.Array[count + 6]);
			SetupLitCornerVertex(x, y + 1, z + 1, color2, textureSlot, 3, ref vertices.Array[count + 7]);
		}
		else
		{
			SetupLitCornerVertex(x, y, z + 1, color2, textureSlot, 1, ref vertices.Array[count + 4]);
			SetupLitCornerVertex(x + 1, y, z, color2, textureSlot, 0, ref vertices.Array[count + 5]);
			SetupLitCornerVertex(x + 1, y + 1, z, color2, textureSlot, 3, ref vertices.Array[count + 6]);
			SetupLitCornerVertex(x, y + 1, z + 1, color2, textureSlot, 2, ref vertices.Array[count + 7]);
		}
		int count2 = indices.Count;
		indices.Count += 24;
		indices.Array[count2] = (ushort)count;
		indices.Array[count2 + 1] = (ushort)(count + 1);
		indices.Array[count2 + 2] = (ushort)(count + 2);
		indices.Array[count2 + 3] = (ushort)(count + 2);
		indices.Array[count2 + 4] = (ushort)(count + 1);
		indices.Array[count2 + 5] = (ushort)count;
		indices.Array[count2 + 6] = (ushort)(count + 2);
		indices.Array[count2 + 7] = (ushort)(count + 3);
		indices.Array[count2 + 8] = (ushort)count;
		indices.Array[count2 + 9] = (ushort)count;
		indices.Array[count2 + 10] = (ushort)(count + 3);
		indices.Array[count2 + 11] = (ushort)(count + 2);
		indices.Array[count2 + 12] = (ushort)(count + 4);
		indices.Array[count2 + 13] = (ushort)(count + 5);
		indices.Array[count2 + 14] = (ushort)(count + 6);
		indices.Array[count2 + 15] = (ushort)(count + 6);
		indices.Array[count2 + 16] = (ushort)(count + 5);
		indices.Array[count2 + 17] = (ushort)(count + 4);
		indices.Array[count2 + 18] = (ushort)(count + 6);
		indices.Array[count2 + 19] = (ushort)(count + 7);
		indices.Array[count2 + 20] = (ushort)(count + 4);
		indices.Array[count2 + 21] = (ushort)(count + 4);
		indices.Array[count2 + 22] = (ushort)(count + 7);
		indices.Array[count2 + 23] = (ushort)(count + 6);
	}

	public void GenerateCubeVertices(Block block, int value, int x, int y, int z, Color color, TerrainGeometrySubset[] subsetsByFace)
	{
		int blockIndex = block.BlockIndex;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			TerrainGeometryDynamicArray<ushort> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, 0, faceTextureSlot, color, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, 1, faceTextureSlot, color, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, 1f, 2, faceTextureSlot, color, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, 1f, 3, faceTextureSlot, color, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = (ushort)count;
			indices.Array[count2 + 1] = (ushort)(count + 2);
			indices.Array[count2 + 2] = (ushort)(count + 1);
			indices.Array[count2 + 3] = (ushort)(count + 2);
			indices.Array[count2 + 4] = (ushort)count;
			indices.Array[count2 + 5] = (ushort)(count + 3);
		}
		cellValueFast = chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 1, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			TerrainGeometryDynamicArray<ushort> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, 1, faceTextureSlot2, color, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, 1f, 2, faceTextureSlot2, color, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, 1f, 3, faceTextureSlot2, color, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, 0, faceTextureSlot2, color, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = (ushort)count3;
			indices2.Array[count4 + 1] = (ushort)(count3 + 2);
			indices2.Array[count4 + 2] = (ushort)(count3 + 1);
			indices2.Array[count4 + 3] = (ushort)(count3 + 2);
			indices2.Array[count4 + 4] = (ushort)count3;
			indices2.Array[count4 + 5] = (ushort)(count3 + 3);
		}
		cellValueFast = chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 2, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			TerrainGeometryDynamicArray<ushort> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, 1, faceTextureSlot3, color, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, 0, faceTextureSlot3, color, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, 1f, 3, faceTextureSlot3, color, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, 1f, 2, faceTextureSlot3, color, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = (ushort)count5;
			indices3.Array[count6 + 1] = (ushort)(count5 + 1);
			indices3.Array[count6 + 2] = (ushort)(count5 + 2);
			indices3.Array[count6 + 3] = (ushort)(count5 + 2);
			indices3.Array[count6 + 4] = (ushort)(count5 + 3);
			indices3.Array[count6 + 5] = (ushort)count5;
		}
		cellValueFast = chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 3, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			TerrainGeometryDynamicArray<ushort> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, 0, faceTextureSlot4, color, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, 1f, 3, faceTextureSlot4, color, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, 1f, 2, faceTextureSlot4, color, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, 1, faceTextureSlot4, color, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = (ushort)count7;
			indices4.Array[count8 + 1] = (ushort)(count7 + 1);
			indices4.Array[count8 + 2] = (ushort)(count7 + 2);
			indices4.Array[count8 + 3] = (ushort)(count7 + 2);
			indices4.Array[count8 + 4] = (ushort)(count7 + 3);
			indices4.Array[count8 + 5] = (ushort)count7;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 4, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			TerrainGeometryDynamicArray<ushort> indices5 = subsetsByFace[4].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(4, value);
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, 1f, 3, faceTextureSlot5, color, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, 1f, 2, faceTextureSlot5, color, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, 1f, 1, faceTextureSlot5, color, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, 1f, 0, faceTextureSlot5, color, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = (ushort)count9;
			indices5.Array[count10 + 1] = (ushort)(count9 + 1);
			indices5.Array[count10 + 2] = (ushort)(count9 + 2);
			indices5.Array[count10 + 3] = (ushort)(count9 + 2);
			indices5.Array[count10 + 4] = (ushort)(count9 + 3);
			indices5.Array[count10 + 5] = (ushort)count9;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			TerrainGeometryDynamicArray<ushort> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot6 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, 0, faceTextureSlot6, color, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, 1, faceTextureSlot6, color, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, 2, faceTextureSlot6, color, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, 3, faceTextureSlot6, color, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = (ushort)count11;
			indices6.Array[count12 + 1] = (ushort)(count11 + 2);
			indices6.Array[count12 + 2] = (ushort)(count11 + 1);
			indices6.Array[count12 + 3] = (ushort)(count11 + 2);
			indices6.Array[count12 + 4] = (ushort)count11;
			indices6.Array[count12 + 5] = (ushort)(count11 + 3);
		}
	}

	public void GenerateCubeVertices(Block block, int value, int x, int y, int z, float height11, float height21, float height22, float height12, Color sideColor, Color topColor11, Color topColor21, Color topColor22, Color topColor12, int overrideTopTextureSlot, TerrainGeometrySubset[] subsetsByFace)
	{
		int blockIndex = block.BlockIndex;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			TerrainGeometryDynamicArray<ushort> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, 0, faceTextureSlot, sideColor, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, 1, faceTextureSlot, sideColor, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, height22, 2, faceTextureSlot, sideColor, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, height12, 3, faceTextureSlot, sideColor, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = (ushort)count;
			indices.Array[count2 + 1] = (ushort)(count + 2);
			indices.Array[count2 + 2] = (ushort)(count + 1);
			indices.Array[count2 + 3] = (ushort)(count + 2);
			indices.Array[count2 + 4] = (ushort)count;
			indices.Array[count2 + 5] = (ushort)(count + 3);
		}
		cellValueFast = chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 1, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			TerrainGeometryDynamicArray<ushort> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, 1, faceTextureSlot2, sideColor, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, height21, 2, faceTextureSlot2, sideColor, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, height22, 3, faceTextureSlot2, sideColor, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, 0, faceTextureSlot2, sideColor, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = (ushort)count3;
			indices2.Array[count4 + 1] = (ushort)(count3 + 2);
			indices2.Array[count4 + 2] = (ushort)(count3 + 1);
			indices2.Array[count4 + 3] = (ushort)(count3 + 2);
			indices2.Array[count4 + 4] = (ushort)count3;
			indices2.Array[count4 + 5] = (ushort)(count3 + 3);
		}
		cellValueFast = chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 2, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			TerrainGeometryDynamicArray<ushort> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, 1, faceTextureSlot3, sideColor, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, 0, faceTextureSlot3, sideColor, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, height21, 3, faceTextureSlot3, sideColor, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, height11, 2, faceTextureSlot3, sideColor, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = (ushort)count5;
			indices3.Array[count6 + 1] = (ushort)(count5 + 1);
			indices3.Array[count6 + 2] = (ushort)(count5 + 2);
			indices3.Array[count6 + 3] = (ushort)(count5 + 2);
			indices3.Array[count6 + 4] = (ushort)(count5 + 3);
			indices3.Array[count6 + 5] = (ushort)count5;
		}
		cellValueFast = chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 3, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			TerrainGeometryDynamicArray<ushort> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, 0, faceTextureSlot4, sideColor, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, height11, 3, faceTextureSlot4, sideColor, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, height12, 2, faceTextureSlot4, sideColor, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, 1, faceTextureSlot4, sideColor, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = (ushort)count7;
			indices4.Array[count8 + 1] = (ushort)(count7 + 1);
			indices4.Array[count8 + 2] = (ushort)(count7 + 2);
			indices4.Array[count8 + 3] = (ushort)(count7 + 2);
			indices4.Array[count8 + 4] = (ushort)(count7 + 3);
			indices4.Array[count8 + 5] = (ushort)count7;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF);
		if (((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 4, value, cellValueFast, x, y, z)) || height11 < 1f || height12 < 1f || height21 < 1f || height22 < 1f)
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			TerrainGeometryDynamicArray<ushort> indices5 = subsetsByFace[4].Indices;
			int textureSlot = ((overrideTopTextureSlot >= 0) ? overrideTopTextureSlot : block.GetFaceTextureSlot(4, value));
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, height11, 3, textureSlot, topColor11, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, height21, 2, textureSlot, topColor21, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, height22, 1, textureSlot, topColor22, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, height12, 0, textureSlot, topColor12, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = (ushort)count9;
			indices5.Array[count10 + 1] = (ushort)(count9 + 1);
			indices5.Array[count10 + 2] = (ushort)(count9 + 2);
			indices5.Array[count10 + 3] = (ushort)(count9 + 2);
			indices5.Array[count10 + 4] = (ushort)(count9 + 3);
			indices5.Array[count10 + 5] = (ushort)count9;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			TerrainGeometryDynamicArray<ushort> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, 0, faceTextureSlot5, sideColor, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, 1, faceTextureSlot5, sideColor, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, 2, faceTextureSlot5, sideColor, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, 3, faceTextureSlot5, sideColor, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = (ushort)count11;
			indices6.Array[count12 + 1] = (ushort)(count11 + 2);
			indices6.Array[count12 + 2] = (ushort)(count11 + 1);
			indices6.Array[count12 + 3] = (ushort)(count11 + 2);
			indices6.Array[count12 + 4] = (ushort)count11;
			indices6.Array[count12 + 5] = (ushort)(count11 + 3);
		}
	}

	public void GenerateCubeVertices(Block block, int value, int x, int y, int z, int rotationX, int rotationY, int rotationZ, Color color, TerrainGeometrySubset[] subsetsByFace)
	{
		int blockIndex = block.BlockIndex;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			TerrainGeometryDynamicArray<ushort> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, rotationZ, faceTextureSlot, color, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, 1 + rotationZ, faceTextureSlot, color, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, 1f, 2 + rotationZ, faceTextureSlot, color, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, 1f, 3 + rotationZ, faceTextureSlot, color, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = (ushort)count;
			indices.Array[count2 + 1] = (ushort)(count + 2);
			indices.Array[count2 + 2] = (ushort)(count + 1);
			indices.Array[count2 + 3] = (ushort)(count + 2);
			indices.Array[count2 + 4] = (ushort)count;
			indices.Array[count2 + 5] = (ushort)(count + 3);
		}
		cellValueFast = chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 1, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			TerrainGeometryDynamicArray<ushort> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, 1 + rotationX, faceTextureSlot2, color, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, 1f, 2 + rotationX, faceTextureSlot2, color, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, 1f, 3 + rotationX, faceTextureSlot2, color, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, rotationX, faceTextureSlot2, color, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = (ushort)count3;
			indices2.Array[count4 + 1] = (ushort)(count3 + 2);
			indices2.Array[count4 + 2] = (ushort)(count3 + 1);
			indices2.Array[count4 + 3] = (ushort)(count3 + 2);
			indices2.Array[count4 + 4] = (ushort)count3;
			indices2.Array[count4 + 5] = (ushort)(count3 + 3);
		}
		cellValueFast = chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 2, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			TerrainGeometryDynamicArray<ushort> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, 1 + rotationZ, faceTextureSlot3, color, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, rotationZ, faceTextureSlot3, color, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, 1f, 3 + rotationZ, faceTextureSlot3, color, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, 1f, 2 + rotationZ, faceTextureSlot3, color, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = (ushort)count5;
			indices3.Array[count6 + 1] = (ushort)(count5 + 1);
			indices3.Array[count6 + 2] = (ushort)(count5 + 2);
			indices3.Array[count6 + 3] = (ushort)(count5 + 2);
			indices3.Array[count6 + 4] = (ushort)(count5 + 3);
			indices3.Array[count6 + 5] = (ushort)count5;
		}
		cellValueFast = chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 3, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			TerrainGeometryDynamicArray<ushort> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, rotationX, faceTextureSlot4, color, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, 1f, 3 + rotationX, faceTextureSlot4, color, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, 1f, 2 + rotationX, faceTextureSlot4, color, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, 1 + rotationX, faceTextureSlot4, color, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = (ushort)count7;
			indices4.Array[count8 + 1] = (ushort)(count7 + 1);
			indices4.Array[count8 + 2] = (ushort)(count7 + 2);
			indices4.Array[count8 + 3] = (ushort)(count7 + 2);
			indices4.Array[count8 + 4] = (ushort)(count7 + 3);
			indices4.Array[count8 + 5] = (ushort)count7;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 4, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			TerrainGeometryDynamicArray<ushort> indices5 = subsetsByFace[4].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(4, value);
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, 1f, 3 + rotationY, faceTextureSlot5, color, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, 1f, 2 + rotationY, faceTextureSlot5, color, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, 1f, 1 + rotationY, faceTextureSlot5, color, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, 1f, rotationY, faceTextureSlot5, color, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = (ushort)count9;
			indices5.Array[count10 + 1] = (ushort)(count9 + 1);
			indices5.Array[count10 + 2] = (ushort)(count9 + 2);
			indices5.Array[count10 + 3] = (ushort)(count9 + 2);
			indices5.Array[count10 + 4] = (ushort)(count9 + 3);
			indices5.Array[count10 + 5] = (ushort)count9;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if ((block.GenerateFacesForSameNeighbors || Terrain.ExtractContents(cellValueFast) != blockIndex) && block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast, x, y, z))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			TerrainGeometryDynamicArray<ushort> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot6 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, rotationY, faceTextureSlot6, color, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, 1 + rotationY, faceTextureSlot6, color, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, 2 + rotationY, faceTextureSlot6, color, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, 3 + rotationY, faceTextureSlot6, color, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = (ushort)count11;
			indices6.Array[count12 + 1] = (ushort)(count11 + 2);
			indices6.Array[count12 + 2] = (ushort)(count11 + 1);
			indices6.Array[count12 + 3] = (ushort)(count11 + 2);
			indices6.Array[count12 + 4] = (ushort)count11;
			indices6.Array[count12 + 5] = (ushort)(count11 + 3);
		}
	}

	public void GenerateFlatVertices(Block block, int value, int x, int y, int z, int rotation, Color color, TerrainGeometrySubset[] subsetsByFace)
	{
		DynamicArray<TerrainVertex> vertices = subsetsByFace[4].Vertices;
		TerrainGeometryDynamicArray<ushort> indices = subsetsByFace[4].Indices;
		int faceTextureSlot = block.GetFaceTextureSlot(4, value);
		int count = vertices.Count;
		vertices.Count += 4;
		SetupCubeVertexFace4(x, y + 1, z, 0f, 3 + rotation, faceTextureSlot, color, ref vertices.Array[count]);
		SetupCubeVertexFace4(x + 1, y + 1, z, 0f, 2 + rotation, faceTextureSlot, color, ref vertices.Array[count + 1]);
		SetupCubeVertexFace4(x + 1, y + 1, z + 1, 0f, 1 + rotation, faceTextureSlot, color, ref vertices.Array[count + 2]);
		SetupCubeVertexFace4(x, y + 1, z + 1, 0f, rotation, faceTextureSlot, color, ref vertices.Array[count + 3]);
		int count2 = indices.Count;
		indices.Count += 6;
		indices.Array[count2] = (ushort)count;
		indices.Array[count2 + 1] = (ushort)(count + 1);
		indices.Array[count2 + 2] = (ushort)(count + 2);
		indices.Array[count2 + 3] = (ushort)(count + 2);
		indices.Array[count2 + 4] = (ushort)(count + 3);
		indices.Array[count2 + 5] = (ushort)count;
	}

	public void GenerateMeshVertices(Block block, int x, int y, int z, BlockMesh blockMesh, Color color, Matrix? matrix, TerrainGeometrySubset subset)
	{
		DynamicArray<TerrainVertex> vertices = subset.Vertices;
		DynamicArray<ushort> indices = subset.Indices;
		int count = vertices.Count;
		int cellLightFast = Terrain.GetCellLightFast(x, y, z);
		float num = LightingManager.LightIntensityByLightValue[cellLightFast];
		vertices.Count += blockMesh.Vertices.Count;
		for (int i = 0; i < blockMesh.Vertices.Count; i++)
		{
			BlockMeshVertex blockMeshVertex = blockMesh.Vertices.Array[i];
			Vector3 vector = blockMeshVertex.Position;
			if (matrix.HasValue)
			{
				vector = Vector3.Transform(blockMeshVertex.Position, matrix.Value);
			}
			Color color2;
			if (blockMeshVertex.IsEmissive)
			{
				color2 = new Color((byte)(color.R * blockMeshVertex.Color.R / 255), (byte)(color.G * blockMeshVertex.Color.G / 255), (byte)(color.B * blockMeshVertex.Color.B / 255));
			}
			else
			{
				float num2 = num / 255f;
				color2 = new Color((byte)((float)(color.R * blockMeshVertex.Color.R) * num2), (byte)((float)(color.G * blockMeshVertex.Color.G) * num2), (byte)((float)(color.B * blockMeshVertex.Color.B) * num2));
			}
			SetupVertex((float)x + vector.X, (float)y + vector.Y, (float)z + vector.Z, color2, blockMeshVertex.TextureCoordinates.X, blockMeshVertex.TextureCoordinates.Y, ref vertices.Array[count + i]);
		}
		if (blockMesh.Sides != null)
		{
			for (int j = 0; j < 6; j++)
			{
				Point3 point = CellFace.FaceToPoint3(j);
				int cellValueFastChunkExists = Terrain.GetCellValueFastChunkExists(x + point.X, y + point.Y, z + point.Z);
				m_visibleSides[j] = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFastChunkExists)].IsFaceTransparent(SubsystemTerrain, CellFace.OppositeFace(j), cellValueFastChunkExists);
			}
			for (int k = 0; k < blockMesh.Indices.Count / 3; k++)
			{
				int num3 = ((blockMesh.Sides == null) ? (-1) : blockMesh.Sides.Array[k]);
				if (num3 < 0 || m_visibleSides[num3])
				{
					indices.Add((ushort)(blockMesh.Indices.Array[3 * k] + count));
					indices.Add((ushort)(blockMesh.Indices.Array[3 * k + 1] + count));
					indices.Add((ushort)(blockMesh.Indices.Array[3 * k + 2] + count));
				}
			}
		}
		else
		{
			for (int l = 0; l < blockMesh.Indices.Count; l++)
			{
				indices.Add((ushort)(blockMesh.Indices.Array[l] + count));
			}
		}
	}

	public void GenerateShadedMeshVertices(Block block, int x, int y, int z, BlockMesh blockMesh, Color color, Matrix? matrix, int[] facesMap, TerrainGeometrySubset subset)
	{
		CalculateCornerLights(x, y, z);
		DynamicArray<TerrainVertex> vertices = subset.Vertices;
		DynamicArray<ushort> indices = subset.Indices;
		int count = vertices.Count;
		vertices.Count += blockMesh.Vertices.Count;
		for (int i = 0; i < blockMesh.Vertices.Count; i++)
		{
			BlockMeshVertex blockMeshVertex = blockMesh.Vertices.Array[i];
			Vector3 vector = blockMeshVertex.Position;
			if (matrix.HasValue)
			{
				vector = Vector3.Transform(vector, matrix.Value);
			}
			Color color2;
			if (blockMeshVertex.IsEmissive)
			{
				color2 = new Color((byte)(color.R * blockMeshVertex.Color.R / 255), (byte)(color.G * blockMeshVertex.Color.G / 255), (byte)(color.B * blockMeshVertex.Color.B / 255));
			}
			else
			{
				int face = ((facesMap != null) ? facesMap[blockMeshVertex.Face] : blockMeshVertex.Face);
				float num = InterpolateCornerLights(face, vector) / 255f;
				color2 = new Color((byte)((float)(color.R * blockMeshVertex.Color.R) * num), (byte)((float)(color.G * blockMeshVertex.Color.G) * num), (byte)((float)(color.B * blockMeshVertex.Color.B) * num));
			}
			SetupVertex((float)x + vector.X, (float)y + vector.Y, (float)z + vector.Z, color2, blockMeshVertex.TextureCoordinates.X, blockMeshVertex.TextureCoordinates.Y, ref vertices.Array[count + i]);
		}
		if (blockMesh.Sides != null)
		{
			for (int j = 0; j < 6; j++)
			{
				Point3 point = CellFace.FaceToPoint3(j);
				int cellValueFastChunkExists = Terrain.GetCellValueFastChunkExists(x + point.X, y + point.Y, z + point.Z);
				m_visibleSides[j] = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFastChunkExists)].IsFaceTransparent(SubsystemTerrain, CellFace.OppositeFace(j), cellValueFastChunkExists);
			}
			for (int k = 0; k < blockMesh.Indices.Count / 3; k++)
			{
				int num2 = ((blockMesh.Sides == null) ? (-1) : blockMesh.Sides.Array[k]);
				if (num2 < 0 || m_visibleSides[(facesMap != null) ? facesMap[num2] : num2])
				{
					indices.Add((ushort)(blockMesh.Indices.Array[3 * k] + count));
					indices.Add((ushort)(blockMesh.Indices.Array[3 * k + 1] + count));
					indices.Add((ushort)(blockMesh.Indices.Array[3 * k + 2] + count));
				}
			}
		}
		else
		{
			for (int l = 0; l < blockMesh.Indices.Count; l++)
			{
				indices.Add((ushort)(blockMesh.Indices.Array[l] + count));
			}
		}
	}

	public void GenerateWireVertices(int value, int x, int y, int z, int mountingFace, float centerBoxSize, Vector2 centerOffset, TerrainGeometrySubset subset)
	{
		if (SubsystemElectricity == null)
		{
			return;
		}
		Color color = WireBlock.WireColor;
		int num = Terrain.ExtractContents(value);
		if (num == 133)
		{
			int? color2 = WireBlock.GetColor(Terrain.ExtractData(value));
			if (color2.HasValue)
			{
				color = SubsystemPalette.GetColor(this, color2);
			}
		}
		int num2 = Terrain.ExtractLight(value);
		float num3 = LightingManager.LightIntensityByLightValue[num2];
		Vector3 vector = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f) - 0.5f * CellFace.FaceToVector3(mountingFace);
		Vector3 vector2 = CellFace.FaceToVector3(mountingFace);
		Vector2 vector3 = new Vector2(0.9376f, 0.0001f);
		Vector2 vector4 = new Vector2(1f / 32f, 0.0055078124f);
		Point3 point = CellFace.FaceToPoint3(mountingFace);
		int cellContents = Terrain.GetCellContents(x - point.X, y - point.Y, z - point.Z);
		bool flag = cellContents == 2 || cellContents == 7 || cellContents == 8 || cellContents == 6 || cellContents == 62 || cellContents == 72;
		Vector3 vector5 = CellFace.FaceToVector3(SubsystemElectricity.GetConnectorFace(mountingFace, ElectricConnectorDirection.Top));
		Vector3 vector6 = CellFace.FaceToVector3(SubsystemElectricity.GetConnectorFace(mountingFace, ElectricConnectorDirection.Left)) * centerOffset.X + vector5 * centerOffset.Y;
		int num4 = 0;
		m_tmpConnectionPaths.Clear();
		SubsystemElectricity.GetAllConnectedNeighbors(x, y, z, mountingFace, m_tmpConnectionPaths);
		foreach (ElectricConnectionPath tmpConnectionPath in m_tmpConnectionPaths)
		{
			if ((num4 & (1 << tmpConnectionPath.ConnectorFace)) != 0)
			{
				continue;
			}
			ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(mountingFace, 0, tmpConnectionPath.ConnectorFace);
			if (centerOffset == Vector2.Zero && connectorDirection == ElectricConnectorDirection.In)
			{
				continue;
			}
			num4 |= 1 << tmpConnectionPath.ConnectorFace;
			Color color3 = color;
			if (num != 133)
			{
				int cellValue = Terrain.GetCellValue(x + tmpConnectionPath.NeighborOffsetX, y + tmpConnectionPath.NeighborOffsetY, z + tmpConnectionPath.NeighborOffsetZ);
				if (Terrain.ExtractContents(cellValue) == 133)
				{
					int? color4 = WireBlock.GetColor(Terrain.ExtractData(cellValue));
					if (color4.HasValue)
					{
						color3 = SubsystemPalette.GetColor(this, color4);
					}
				}
			}
			Vector3 vector7 = ((connectorDirection != ElectricConnectorDirection.In) ? CellFace.FaceToVector3(tmpConnectionPath.ConnectorFace) : (-Vector3.Normalize(vector6)));
			Vector3 vector8 = Vector3.Cross(vector2, vector7);
			float num5 = ((centerBoxSize >= 0f) ? MathUtils.Max(1f / 32f, centerBoxSize / 2f) : (centerBoxSize / 2f));
			float num6 = ((connectorDirection == ElectricConnectorDirection.In) ? (1f / 32f) : 0.5f);
			float num7 = ((connectorDirection == ElectricConnectorDirection.In) ? 0f : ((tmpConnectionPath.ConnectorFace == tmpConnectionPath.NeighborFace) ? (num6 + 1f / 32f) : ((tmpConnectionPath.ConnectorFace != CellFace.OppositeFace(tmpConnectionPath.NeighborFace)) ? num6 : (num6 - 1f / 32f))));
			Vector3 vector9 = vector - vector8 * (1f / 32f) + vector7 * num5 + vector6;
			Vector3 vector10 = vector - vector8 * (1f / 32f) + vector7 * num6;
			Vector3 vector11 = vector + vector8 * (1f / 32f) + vector7 * num6;
			Vector3 vector12 = vector + vector8 * (1f / 32f) + vector7 * num5 + vector6;
			Vector3 vector13 = vector + vector2 * (1f / 32f) + vector7 * (centerBoxSize / 2f) + vector6;
			Vector3 vector14 = vector + vector2 * (1f / 32f) + vector7 * num7;
			if (flag && centerBoxSize == 0f)
			{
				Vector3 vector15 = 0.25f * GetRandomWireOffset(0.5f * (vector9 + vector12), vector2);
				vector9 += vector15;
				vector12 += vector15;
				vector13 += vector15;
			}
			Vector2 vector16 = vector3 + vector4 * new Vector2(MathUtils.Max(0.0625f, centerBoxSize), 0f);
			Vector2 vector17 = vector3 + vector4 * new Vector2(num6 * 2f, 0f);
			Vector2 vector18 = vector3 + vector4 * new Vector2(num6 * 2f, 1f);
			Vector2 vector19 = vector3 + vector4 * new Vector2(MathUtils.Max(0.0625f, centerBoxSize), 1f);
			Vector2 vector20 = vector3 + vector4 * new Vector2(centerBoxSize, 0.5f);
			Vector2 vector21 = vector3 + vector4 * new Vector2(num7 * 2f, 0.5f);
			int num8 = Terrain.ExtractLight(Terrain.GetCellValue(x + tmpConnectionPath.NeighborOffsetX, y + tmpConnectionPath.NeighborOffsetY, z + tmpConnectionPath.NeighborOffsetZ));
			float num9 = LightingManager.LightIntensityByLightValue[num8];
			float num10 = 0.5f * (num3 + num9);
			float num11 = LightingManager.CalculateLighting(-vector8);
			float num12 = LightingManager.CalculateLighting(vector8);
			float num13 = LightingManager.CalculateLighting(vector2);
			float num14 = num11 * num3;
			float num15 = num11 * num10;
			float num16 = num12 * num10;
			float num17 = num12 * num3;
			float num18 = num13 * num3;
			float num19 = num13 * num10;
			Color color5 = new Color((byte)((float)(int)color3.R * num14), (byte)((float)(int)color3.G * num14), (byte)((float)(int)color3.B * num14));
			Color color6 = new Color((byte)((float)(int)color3.R * num15), (byte)((float)(int)color3.G * num15), (byte)((float)(int)color3.B * num15));
			Color color7 = new Color((byte)((float)(int)color3.R * num16), (byte)((float)(int)color3.G * num16), (byte)((float)(int)color3.B * num16));
			Color color8 = new Color((byte)((float)(int)color3.R * num17), (byte)((float)(int)color3.G * num17), (byte)((float)(int)color3.B * num17));
			Color color9 = new Color((byte)((float)(int)color3.R * num18), (byte)((float)(int)color3.G * num18), (byte)((float)(int)color3.B * num18));
			Color color10 = new Color((byte)((float)(int)color3.R * num19), (byte)((float)(int)color3.G * num19), (byte)((float)(int)color3.B * num19));
			int count = subset.Vertices.Count;
			subset.Vertices.Count += 6;
			TerrainVertex[] array = subset.Vertices.Array;
			SetupVertex(vector9.X, vector9.Y, vector9.Z, color5, vector16.X, vector16.Y, ref array[count]);
			SetupVertex(vector10.X, vector10.Y, vector10.Z, color6, vector17.X, vector17.Y, ref array[count + 1]);
			SetupVertex(vector11.X, vector11.Y, vector11.Z, color7, vector18.X, vector18.Y, ref array[count + 2]);
			SetupVertex(vector12.X, vector12.Y, vector12.Z, color8, vector19.X, vector19.Y, ref array[count + 3]);
			SetupVertex(vector13.X, vector13.Y, vector13.Z, color9, vector20.X, vector20.Y, ref array[count + 4]);
			SetupVertex(vector14.X, vector14.Y, vector14.Z, color10, vector21.X, vector21.Y, ref array[count + 5]);
			int count2 = subset.Indices.Count;
			subset.Indices.Count += ((connectorDirection == ElectricConnectorDirection.In) ? 15 : 12);
			ushort[] array2 = subset.Indices.Array;
			array2[count2] = (ushort)count;
			array2[count2 + 1] = (ushort)(count + 5);
			array2[count2 + 2] = (ushort)(count + 1);
			array2[count2 + 3] = (ushort)(count + 5);
			array2[count2 + 4] = (ushort)count;
			array2[count2 + 5] = (ushort)(count + 4);
			array2[count2 + 6] = (ushort)(count + 4);
			array2[count2 + 7] = (ushort)(count + 2);
			array2[count2 + 8] = (ushort)(count + 5);
			array2[count2 + 9] = (ushort)(count + 2);
			array2[count2 + 10] = (ushort)(count + 4);
			array2[count2 + 11] = (ushort)(count + 3);
			if (connectorDirection == ElectricConnectorDirection.In)
			{
				array2[count2 + 12] = (ushort)(count + 2);
				array2[count2 + 13] = (ushort)(count + 1);
				array2[count2 + 14] = (ushort)(count + 5);
			}
		}
		if (centerBoxSize != 0f || (num4 == 0 && num != 133))
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			if (i != mountingFace && i != CellFace.OppositeFace(mountingFace) && (num4 & (1 << i)) == 0)
			{
				Vector3 vector22 = CellFace.FaceToVector3(i);
				Vector3 vector23 = Vector3.Cross(vector2, vector22);
				Vector3 vector24 = vector - vector23 * (1f / 32f) + vector22 * (1f / 32f);
				Vector3 vector25 = vector + vector23 * (1f / 32f) + vector22 * (1f / 32f);
				Vector3 vector26 = vector + vector2 * (1f / 32f);
				if (flag)
				{
					Vector3 vector27 = 0.25f * GetRandomWireOffset(0.5f * (vector24 + vector25), vector2);
					vector24 += vector27;
					vector25 += vector27;
					vector26 += vector27;
				}
				Vector2 vector28 = vector3 + vector4 * new Vector2(0.0625f, 0f);
				Vector2 vector29 = vector3 + vector4 * new Vector2(0.0625f, 1f);
				Vector2 vector30 = vector3 + vector4 * new Vector2(0f, 0.5f);
				float num20 = LightingManager.CalculateLighting(vector22) * num3;
				float num21 = LightingManager.CalculateLighting(vector2) * num3;
				Color color11 = new Color((byte)((float)(int)color.R * num20), (byte)((float)(int)color.G * num20), (byte)((float)(int)color.B * num20));
				Color color12 = new Color((byte)((float)(int)color.R * num21), (byte)((float)(int)color.G * num21), (byte)((float)(int)color.B * num21));
				int count3 = subset.Vertices.Count;
				subset.Vertices.Count += 3;
				TerrainVertex[] array3 = subset.Vertices.Array;
				SetupVertex(vector24.X, vector24.Y, vector24.Z, color11, vector28.X, vector28.Y, ref array3[count3]);
				SetupVertex(vector25.X, vector25.Y, vector25.Z, color11, vector29.X, vector29.Y, ref array3[count3 + 1]);
				SetupVertex(vector26.X, vector26.Y, vector26.Z, color12, vector30.X, vector30.Y, ref array3[count3 + 2]);
				int count4 = subset.Indices.Count;
				subset.Indices.Count += 3;
				ushort[] array4 = subset.Indices.Array;
				array4[count4] = (ushort)count3;
				array4[count4 + 1] = (ushort)(count3 + 2);
				array4[count4 + 2] = (ushort)(count3 + 1);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void CalculateCubeVertexLight(int value, ref int light, ref int shadow)
	{
		int num = Terrain.ExtractContents(value);
		if (num == 0)
		{
			light = Math.Max(light, Terrain.ExtractLight(value));
			return;
		}
		light = Math.Max(light, Terrain.ExtractLight(value));
		shadow += BlocksManager.Blocks[num].GetShadowStrength(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int CombineLightAndShadow(int light, int shadow)
	{
		return MathUtils.Max(light - MathUtils.Max(shadow / 6, 0), 0);
	}

	private int CalculateVertexLightFace0(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z);
		int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, z & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
		int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
		CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z);
		int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
		int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
		int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
		CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	private int CalculateVertexLightFace1(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z - 1);
		int num = TerrainChunk.CalculateCellIndex(x & 0xF, y, (z - 1) & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
		int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
		CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z);
		int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
		int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
		int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
		CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	private int CalculateVertexLightFace2(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z - 1);
		int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, (z - 1) & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
		int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
		CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z - 1);
		int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, (z - 1) & 0xF);
		int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
		int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
		CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	private int CalculateVertexLightFace3(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z - 1);
		int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, (z - 1) & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
		int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
		CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x - 1, z);
		int num2 = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, z & 0xF);
		int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
		int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
		CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	private int CalculateVertexLightFace4(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y, z - 1), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y, z - 1), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y, z), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y, z), ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	private int CalculateVertexLightFace5(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y - 1, z - 1), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y - 1, z - 1), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y - 1, z), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y - 1, z), ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	private void SetupCubeVertexFace0(int x, int y, int z, float height, int corner, int textureSlot, Color color, ref TerrainVertex vertex)
	{
		int num = CalculateVertexLightFace0(x, y, z);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % 16)) / 16f;
		float ty = (m_textureCoordinates[corner].Y + (1f - height) + (float)(textureSlot / 16)) / 16f;
		SetupVertex(x, (float)y + height - 1f, z, color2, tx, ty, ref vertex);
	}

	private void SetupCubeVertexFace1(int x, int y, int z, float height, int corner, int textureSlot, Color color, ref TerrainVertex vertex)
	{
		int num = CalculateVertexLightFace1(x, y, z);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 16];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % 16)) / 16f;
		float ty = (m_textureCoordinates[corner].Y + (1f - height) + (float)(textureSlot / 16)) / 16f;
		SetupVertex(x, (float)y + height - 1f, z, color2, tx, ty, ref vertex);
	}

	private void SetupCubeVertexFace2(int x, int y, int z, float height, int corner, int textureSlot, Color color, ref TerrainVertex vertex)
	{
		int num = CalculateVertexLightFace2(x, y, z);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 32];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % 16)) / 16f;
		float ty = (m_textureCoordinates[corner].Y + (1f - height) + (float)(textureSlot / 16)) / 16f;
		SetupVertex(x, (float)y + height - 1f, z, color2, tx, ty, ref vertex);
	}

	private void SetupCubeVertexFace3(int x, int y, int z, float height, int corner, int textureSlot, Color color, ref TerrainVertex vertex)
	{
		int num = CalculateVertexLightFace3(x, y, z);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 48];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % 16)) / 16f;
		float ty = (m_textureCoordinates[corner].Y + (1f - height) + (float)(textureSlot / 16)) / 16f;
		SetupVertex(x, (float)y + height - 1f, z, color2, tx, ty, ref vertex);
	}

	private void SetupCubeVertexFace4(int x, int y, int z, float height, int corner, int textureSlot, Color color, ref TerrainVertex vertex)
	{
		int num = CalculateVertexLightFace4(x, y, z);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 64];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % 16)) / 16f;
		float ty = (m_textureCoordinates[corner].Y + (float)(textureSlot / 16)) / 16f;
		SetupVertex(x, (float)y + height - 1f, z, color2, tx, ty, ref vertex);
	}

	private void SetupCubeVertexFace5(int x, int y, int z, float height, int corner, int textureSlot, Color color, ref TerrainVertex vertex)
	{
		int num = CalculateVertexLightFace5(x, y, z);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 80];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % 16)) / 16f;
		float ty = (m_textureCoordinates[corner].Y + (float)(textureSlot / 16)) / 16f;
		SetupVertex(x, (float)y + height - 1f, z, color2, tx, ty, ref vertex);
	}

	private static Vector3 GetRandomWireOffset(Vector3 position, Vector3 normal)
	{
		int hashCode = Vector3.Round(2f * position).GetHashCode();
		return new Vector3
		{
			X = ((normal.X == 0f) ? ((float)(MathUtils.Hash((uint)hashCode) % 255) / 255f - 0.5f) : 0f),
			Y = ((normal.Y == 0f) ? ((float)(MathUtils.Hash((uint)(hashCode + 1)) % 255) / 255f - 0.5f) : 0f),
			Z = ((normal.Z == 0f) ? ((float)(MathUtils.Hash((uint)(hashCode + 2)) % 255) / 255f - 0.5f) : 0f)
		};
	}

	private void CalculateCornerLights(int x, int y, int z)
	{
		if (!(m_cornerLightsPosition == new Point3(x, y, z)))
		{
			m_cornerLightsPosition = new Point3(x, y, z);
			m_cornerLightsByFace[0].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y, z)];
			m_cornerLightsByFace[0].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y, z + 1)];
			m_cornerLightsByFace[0].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y + 1, z)];
			m_cornerLightsByFace[0].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y + 1, z + 1)];
			m_cornerLightsByFace[0].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y, z)];
			m_cornerLightsByFace[0].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y, z + 1)];
			m_cornerLightsByFace[0].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y + 1, z)];
			m_cornerLightsByFace[0].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[1].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y, z)];
			m_cornerLightsByFace[1].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y, z + 1)];
			m_cornerLightsByFace[1].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y + 1, z)];
			m_cornerLightsByFace[1].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y + 1, z + 1)];
			m_cornerLightsByFace[1].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y, z)];
			m_cornerLightsByFace[1].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y, z + 1)];
			m_cornerLightsByFace[1].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y + 1, z)];
			m_cornerLightsByFace[1].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[2].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y, z)];
			m_cornerLightsByFace[2].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y, z + 1)];
			m_cornerLightsByFace[2].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y + 1, z)];
			m_cornerLightsByFace[2].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y + 1, z + 1)];
			m_cornerLightsByFace[2].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y, z)];
			m_cornerLightsByFace[2].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y, z + 1)];
			m_cornerLightsByFace[2].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y + 1, z)];
			m_cornerLightsByFace[2].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[3].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y, z)];
			m_cornerLightsByFace[3].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y, z + 1)];
			m_cornerLightsByFace[3].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y + 1, z)];
			m_cornerLightsByFace[3].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y + 1, z + 1)];
			m_cornerLightsByFace[3].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y, z)];
			m_cornerLightsByFace[3].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y, z + 1)];
			m_cornerLightsByFace[3].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y + 1, z)];
			m_cornerLightsByFace[3].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[4].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y, z)];
			m_cornerLightsByFace[4].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y, z + 1)];
			m_cornerLightsByFace[4].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y + 1, z)];
			m_cornerLightsByFace[4].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y + 1, z + 1)];
			m_cornerLightsByFace[4].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y, z)];
			m_cornerLightsByFace[4].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y, z + 1)];
			m_cornerLightsByFace[4].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y + 1, z)];
			m_cornerLightsByFace[4].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[5].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y, z)];
			m_cornerLightsByFace[5].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y, z + 1)];
			m_cornerLightsByFace[5].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y + 1, z)];
			m_cornerLightsByFace[5].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y + 1, z + 1)];
			m_cornerLightsByFace[5].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y, z)];
			m_cornerLightsByFace[5].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y, z + 1)];
			m_cornerLightsByFace[5].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y + 1, z)];
			m_cornerLightsByFace[5].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y + 1, z + 1)];
		}
	}

	private float InterpolateCornerLights(int face, Vector3 position)
	{
		float x = position.X;
		float y = position.Y;
		float z = position.Z;
		float num = 1f - x;
		float num2 = 1f - y;
		float num3 = 1f - z;
		return m_cornerLightsByFace[face].L000 * num * num2 * num3 + m_cornerLightsByFace[face].L001 * num * num2 * z + m_cornerLightsByFace[face].L010 * num * y * num3 + m_cornerLightsByFace[face].L011 * num * y * z + m_cornerLightsByFace[face].L100 * x * num2 * num3 + m_cornerLightsByFace[face].L101 * x * num2 * z + m_cornerLightsByFace[face].L110 * x * y * num3 + m_cornerLightsByFace[face].L111 * x * y * z;
	}
}
