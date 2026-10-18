using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class CairnBlock : Block
{
	public const int Index = 258;

	private BlockMesh m_mesh = new BlockMesh();

	private BlockMesh m_standaloneMesh = new BlockMesh();

	private BoundingBox[] m_collisionBoxes;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Cairn");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Cairn").ParentBone);
		Color white = Color.White;
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(model.FindMesh("Cairn").MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, white);
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendModelMeshPart(model.FindMesh("Wood").MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, white);
		m_mesh.AppendBlockMesh(blockMesh);
		m_mesh.AppendBlockMesh(blockMesh2);
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Cairn").MeshParts[0], boneAbsoluteTransform * Matrix.CreateScale(1.3f) * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, white);
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Wood").MeshParts[0], boneAbsoluteTransform * Matrix.CreateScale(1.3f) * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, white);
		m_collisionBoxes = new BoundingBox[1] { blockMesh.CalculateBoundingBox() };
		base.Initialize();
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateMeshVertices(this, x, y, z, m_mesh, Color.White, null, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, color, size, ref matrix, environmentData);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int num = Terrain.ExtractData(oldValue);
		int num2 = 10 + 4 * num;
		int num3 = ((num >= 3) ? 1 : 0);
		for (int i = 0; i < 3; i++)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = 79,
				Count = 1
			});
		}
		for (int j = 0; j < num2; j++)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = 248,
				Count = 1
			});
		}
		for (int k = 0; k < num3; k++)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = 111,
				Count = 1
			});
		}
		for (int l = 0; l < 2; l++)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = 23,
				Count = 1
			});
		}
		showDebris = false;
	}
}
