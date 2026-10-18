using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Engine.Serialization;

namespace Game;

public static class BlocksManager
{
	private struct ImageExtrusionKey
	{
		public Image Image;

		public int Slot;

		public override int GetHashCode()
		{
			return Image.GetHashCode() ^ Slot.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				ImageExtrusionKey imageExtrusionKey = (ImageExtrusionKey)obj;
				if (imageExtrusionKey.Image == Image)
				{
					return imageExtrusionKey.Slot == Slot;
				}
				return false;
			}
			return false;
		}
	}

	private static Block[] m_blocks;

	private static FluidBlock[] m_fluidBlocks;

	private static List<string> m_categories = new List<string>();

	private static DrawBlockEnvironmentData m_defaultEnvironmentData = new DrawBlockEnvironmentData();

	private static Vector4[] m_slotTexCoords = new Vector4[256];

	private static Dictionary<ImageExtrusionKey, BlockMesh> m_imageExtrusionsCache = new Dictionary<ImageExtrusionKey, BlockMesh>();

	public static Block[] Blocks => m_blocks;

	public static FluidBlock[] FluidBlocks => m_fluidBlocks;

	public static ReadOnlyList<string> Categories => new ReadOnlyList<string>(m_categories);

	public static void Initialize()
	{
		GameManager.ProjectDisposed += delegate
		{
			m_imageExtrusionsCache.Clear();
		};
		LoadingManager.QueueItem("CalculateSlotTexCoordTables", delegate
		{
			CalculateSlotTexCoordTables();
		});
		LoadingManager.QueueItem("PopulateBlockArrays", delegate
		{
			int num = 0;
			Dictionary<int, Block> dictionary = new Dictionary<int, Block>();
			foreach (TypeInfo definedType in typeof(BlocksManager).GetTypeInfo().Assembly.DefinedTypes)
			{
				if (definedType.IsSubclassOf(typeof(Block)) && !definedType.IsAbstract)
				{
					FieldInfo fieldInfo = (from fi in definedType.AsType().GetRuntimeFields()
						where fi.Name == "Index" && fi.IsPublic && fi.IsStatic
						select fi).FirstOrDefault();
					if (!(fieldInfo != null) || !(fieldInfo.FieldType == typeof(int)))
					{
						throw new InvalidOperationException($"Block type \"{definedType.FullName}\" does not have static field Index of type int.");
					}
					int num2 = (int)fieldInfo.GetValue(null);
					if (dictionary.ContainsKey(num2))
					{
						throw new InvalidOperationException($"Index of block type \"{definedType.FullName}\" conflicts with another block.");
					}
					Block block = (Block)Activator.CreateInstance(definedType.AsType());
					block.BlockIndex = num2;
					dictionary.Add(num2, block);
					num = MathUtils.Max(num, num2);
				}
			}
			m_blocks = new Block[num + 1];
			m_fluidBlocks = new FluidBlock[num + 1];
			foreach (KeyValuePair<int, Block> item in dictionary)
			{
				m_blocks[item.Key] = item.Value;
				m_fluidBlocks[item.Key] = item.Value as FluidBlock;
			}
			for (int num3 = 0; num3 < m_blocks.Length; num3++)
			{
				if (m_blocks[num3] == null)
				{
					m_blocks[num3] = m_blocks[0];
				}
			}
		});
		LoadingManager.QueueItem("LoadBlocksData", delegate
		{
			string data = ContentManager.Get<string>("BlocksData");
			ContentManager.Dispose("BlocksData");
			LoadBlocksData(data);
			Block[] blocks = Blocks;
			foreach (Block block in blocks)
			{
				LoadingManager.QueueItem("Initialize " + block.GetType().Name, delegate
				{
					block.Initialize();
				});
			}
		});
		LoadingManager.QueueItem("SetupBlockCategories", delegate
		{
			m_categories.Add("Terrain");
			m_categories.Add("Minerals");
			m_categories.Add("Plants");
			m_categories.Add("Construction");
			m_categories.Add("Items");
			m_categories.Add("Tools");
			m_categories.Add("Weapons");
			m_categories.Add("Clothes");
			m_categories.Add("Electrics");
			m_categories.Add("Food");
			m_categories.Add("Spawner Eggs");
			m_categories.Add("Painted");
			m_categories.Add("Dyed");
			m_categories.Add("Fireworks");
			Block[] blocks = Blocks;
			foreach (Block block in blocks)
			{
				foreach (int creativeValue in block.GetCreativeValues())
				{
					string category = block.GetCategory(creativeValue);
					if (!m_categories.Contains(category))
					{
						m_categories.Add(category);
					}
				}
			}
		});
	}

	public static Block FindBlockByTypeName(string typeName, bool throwIfNotFound)
	{
		Block? block = Blocks.FirstOrDefault((Block b) => b.GetType().Name == typeName);
		if ((block == null) & throwIfNotFound)
		{
			throw new InvalidOperationException($"Block with type {typeName} not found.");
		}
		return block;
	}

	public static Block[] FindBlocksByCraftingId(string craftingId)
	{
		return Blocks.Where((Block b) => b.CraftingId == craftingId).ToArray();
	}

	public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer, int value, Vector3 size, float height, ref Matrix matrix, Color color, Color topColor, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		Texture2D texture = ((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
		TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.PointClamp);
		float s = LightingManager.LightIntensityByLightValue[environmentData.Light];
		color = Color.MultiplyColorOnlyNotSaturated(color, s);
		topColor = Color.MultiplyColorOnlyNotSaturated(topColor, s);
		Vector3 translation = matrix.Translation;
		Vector3 vector = matrix.Right * size.X;
		Vector3 vector2 = matrix.Up * size.Y * height;
		Vector3 vector3 = matrix.Forward * size.Z;
		Vector3 v = translation + 0.5f * (-vector - vector2 - vector3);
		Vector3 v2 = translation + 0.5f * (vector - vector2 - vector3);
		Vector3 v3 = translation + 0.5f * (-vector + vector2 - vector3);
		Vector3 v4 = translation + 0.5f * (vector + vector2 - vector3);
		Vector3 v5 = translation + 0.5f * (-vector - vector2 + vector3);
		Vector3 v6 = translation + 0.5f * (vector - vector2 + vector3);
		Vector3 v7 = translation + 0.5f * (-vector + vector2 + vector3);
		Vector3 v8 = translation + 0.5f * (vector + vector2 + vector3);
		if (environmentData.ViewProjectionMatrix.HasValue)
		{
			Matrix m = environmentData.ViewProjectionMatrix.Value;
			Vector3.Transform(ref v, ref m, out v);
			Vector3.Transform(ref v2, ref m, out v2);
			Vector3.Transform(ref v3, ref m, out v3);
			Vector3.Transform(ref v4, ref m, out v4);
			Vector3.Transform(ref v5, ref m, out v5);
			Vector3.Transform(ref v6, ref m, out v6);
			Vector3.Transform(ref v7, ref m, out v7);
			Vector3.Transform(ref v8, ref m, out v8);
		}
		int num = Terrain.ExtractContents(value);
		Block block = Blocks[num];
		Vector4 vector4 = m_slotTexCoords[block.GetFaceTextureSlot(0, value)];
		vector4.W = MathUtils.Lerp(vector4.Y, vector4.W, height);
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(-matrix.Forward)), p1: v, p2: v3, p3: v4, p4: v2, texCoord1: new Vector2(vector4.X, vector4.W), texCoord2: new Vector2(vector4.X, vector4.Y), texCoord3: new Vector2(vector4.Z, vector4.Y), texCoord4: new Vector2(vector4.Z, vector4.W));
		vector4 = m_slotTexCoords[block.GetFaceTextureSlot(2, value)];
		vector4.W = MathUtils.Lerp(vector4.Y, vector4.W, height);
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(matrix.Forward)), p1: v5, p2: v6, p3: v8, p4: v7, texCoord1: new Vector2(vector4.Z, vector4.W), texCoord2: new Vector2(vector4.X, vector4.W), texCoord3: new Vector2(vector4.X, vector4.Y), texCoord4: new Vector2(vector4.Z, vector4.Y));
		vector4 = m_slotTexCoords[block.GetFaceTextureSlot(5, value)];
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(-matrix.Up)), p1: v, p2: v2, p3: v6, p4: v5, texCoord1: new Vector2(vector4.X, vector4.Y), texCoord2: new Vector2(vector4.Z, vector4.Y), texCoord3: new Vector2(vector4.Z, vector4.W), texCoord4: new Vector2(vector4.X, vector4.W));
		vector4 = m_slotTexCoords[block.GetFaceTextureSlot(4, value)];
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnlyNotSaturated(topColor, LightingManager.CalculateLighting(matrix.Up)), p1: v3, p2: v7, p3: v8, p4: v4, texCoord1: new Vector2(vector4.X, vector4.W), texCoord2: new Vector2(vector4.X, vector4.Y), texCoord3: new Vector2(vector4.Z, vector4.Y), texCoord4: new Vector2(vector4.Z, vector4.W));
		vector4 = m_slotTexCoords[block.GetFaceTextureSlot(1, value)];
		vector4.W = MathUtils.Lerp(vector4.Y, vector4.W, height);
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(-matrix.Right)), p1: v, p2: v5, p3: v7, p4: v3, texCoord1: new Vector2(vector4.Z, vector4.W), texCoord2: new Vector2(vector4.X, vector4.W), texCoord3: new Vector2(vector4.X, vector4.Y), texCoord4: new Vector2(vector4.Z, vector4.Y));
		vector4 = m_slotTexCoords[block.GetFaceTextureSlot(3, value)];
		vector4.W = MathUtils.Lerp(vector4.Y, vector4.W, height);
		texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(matrix.Right)), p1: v2, p2: v4, p3: v8, p4: v6, texCoord1: new Vector2(vector4.X, vector4.W), texCoord2: new Vector2(vector4.X, vector4.Y), texCoord3: new Vector2(vector4.Z, vector4.Y), texCoord4: new Vector2(vector4.Z, vector4.W));
	}

	public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer, int value, Vector3 size, ref Matrix matrix, Color color, Color topColor, DrawBlockEnvironmentData environmentData)
	{
		DrawCubeBlock(primitivesRenderer, value, size, 1f, ref matrix, color, topColor, environmentData);
	}

	public static void DrawFlatOrImageExtrusionBlock(PrimitivesRenderer3D primitivesRenderer, int value, float size, ref Matrix matrix, Texture2D texture, Color color, bool isEmissive, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		if (texture == null && !isEmissive && (environmentData.DrawBlockMode == DrawBlockMode.FirstPerson || environmentData.DrawBlockMode == DrawBlockMode.ThirdPerson))
		{
			DrawImageExtrusionBlock(primitivesRenderer, value, size, ref matrix, color, environmentData);
		}
		else
		{
			DrawFlatBlock(primitivesRenderer, value, size, ref matrix, texture, color, isEmissive, environmentData);
		}
	}

	public static void DrawFlatBlock(PrimitivesRenderer3D primitivesRenderer, int value, float size, ref Matrix matrix, Texture2D texture, Color color, bool isEmissive, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		int num = Terrain.ExtractContents(value);
		Block block = Blocks[num];
		Vector4 vector;
		if (texture == null)
		{
			texture = ((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
			vector = m_slotTexCoords[block.GetFaceTextureSlot(-1, value)];
		}
		else
		{
			vector = new Vector4(0f, 0f, 1f, 1f);
		}
		if (!isEmissive)
		{
			float s = LightingManager.LightIntensityByLightValue[environmentData.Light];
			color = Color.MultiplyColorOnly(color, s);
		}
		Vector3 translation = matrix.Translation;
		Vector3 vector2;
		Vector3 vector3;
		if (environmentData.BillboardDirection.HasValue)
		{
			vector2 = Vector3.Normalize(Vector3.Cross(environmentData.BillboardDirection.Value, Vector3.UnitY));
			vector3 = -Vector3.Normalize(Vector3.Cross(environmentData.BillboardDirection.Value, vector2));
		}
		else
		{
			vector2 = matrix.Right;
			vector3 = matrix.Up;
		}
		Vector3 v = translation + 0.85f * size * (-vector2 - vector3);
		Vector3 v2 = translation + 0.85f * size * (vector2 - vector3);
		Vector3 v3 = translation + 0.85f * size * (-vector2 + vector3);
		Vector3 v4 = translation + 0.85f * size * (vector2 + vector3);
		if (environmentData.ViewProjectionMatrix.HasValue)
		{
			Matrix m = environmentData.ViewProjectionMatrix.Value;
			Vector3.Transform(ref v, ref m, out v);
			Vector3.Transform(ref v2, ref m, out v2);
			Vector3.Transform(ref v3, ref m, out v3);
			Vector3.Transform(ref v4, ref m, out v4);
		}
		TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.PointClamp);
		texturedBatch3D.QueueQuad(v, v3, v4, v2, new Vector2(vector.X, vector.W), new Vector2(vector.X, vector.Y), new Vector2(vector.Z, vector.Y), new Vector2(vector.Z, vector.W), color);
		if (!environmentData.BillboardDirection.HasValue)
		{
			texturedBatch3D.QueueQuad(v, v2, v4, v3, new Vector2(vector.X, vector.W), new Vector2(vector.Z, vector.W), new Vector2(vector.Z, vector.Y), new Vector2(vector.X, vector.Y), color);
		}
	}

	public static void DrawImageExtrusionBlock(PrimitivesRenderer3D primitivesRenderer, int value, float size, ref Matrix matrix, Color color, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		int num = Terrain.ExtractContents(value);
		Block block = Blocks[num];
		BlockMesh imageExtrusionBlockMesh = GetImageExtrusionBlockMesh((Image)((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture).Tag, block.GetFaceTextureSlot(-1, value));
		DrawMeshBlock(primitivesRenderer, imageExtrusionBlockMesh, color, 1.7f * size, ref matrix, environmentData);
	}

	public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		Texture2D texture = ((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
		DrawMeshBlock(primitivesRenderer, blockMesh, texture, Color.White, size, ref matrix, environmentData);
	}

	public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		Texture2D texture = ((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
		DrawMeshBlock(primitivesRenderer, blockMesh, texture, color, size, ref matrix, environmentData);
	}

	public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, Texture2D texture, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		environmentData = environmentData ?? m_defaultEnvironmentData;
		float num = LightingManager.LightIntensityByLightValue[environmentData.Light];
		Vector4 vector = new Vector4(color);
		Vector4 vector2 = new Vector4(new Vector3(vector.X, vector.Y, vector.Z) * num, vector.W);
		TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, BlendState.AlphaBlend, SamplerState.PointClamp);
		bool flag = false;
		Matrix m = ((!environmentData.ViewProjectionMatrix.HasValue) ? matrix : (matrix * environmentData.ViewProjectionMatrix.Value));
		if (size != 1f)
		{
			m = Matrix.CreateScale(size) * m;
		}
		if (m.M14 != 0f || m.M24 != 0f || m.M34 != 0f || m.M44 != 1f)
		{
			flag = true;
		}
		int count = blockMesh.Vertices.Count;
		BlockMeshVertex[] array = blockMesh.Vertices.Array;
		int count2 = blockMesh.Indices.Count;
		ushort[] array2 = blockMesh.Indices.Array;
		DynamicArray<VertexPositionColorTexture> triangleVertices = texturedBatch3D.TriangleVertices;
		int count3 = triangleVertices.Count;
		int count4 = triangleVertices.Count;
		triangleVertices.Count += count;
		for (int i = 0; i < count; i++)
		{
			BlockMeshVertex blockMeshVertex = array[i];
			if (flag)
			{
				Vector4 v = new Vector4(blockMeshVertex.Position, 1f);
				Vector4.Transform(ref v, ref m, out v);
				float num2 = 1f / v.W;
				blockMeshVertex.Position = new Vector3(v.X * num2, v.Y * num2, v.Z * num2);
			}
			else
			{
				Vector3.Transform(ref blockMeshVertex.Position, ref m, out blockMeshVertex.Position);
			}
			Color color2 = ((!blockMeshVertex.IsEmissive) ? new Color((byte)((float)(int)blockMeshVertex.Color.R * vector2.X), (byte)((float)(int)blockMeshVertex.Color.G * vector2.Y), (byte)((float)(int)blockMeshVertex.Color.B * vector2.Z), (byte)((float)(int)blockMeshVertex.Color.A * vector2.W)) : new Color((byte)((float)(int)blockMeshVertex.Color.R * vector.X), (byte)((float)(int)blockMeshVertex.Color.G * vector.Y), (byte)((float)(int)blockMeshVertex.Color.B * vector.Z), (byte)((float)(int)blockMeshVertex.Color.A * vector.W)));
			triangleVertices.Array[count4++] = new VertexPositionColorTexture(blockMeshVertex.Position, color2, blockMeshVertex.TextureCoordinates);
		}
		DynamicArray<ushort> triangleIndices = texturedBatch3D.TriangleIndices;
		int count5 = triangleIndices.Count;
		triangleIndices.Count += count2;
		for (int j = 0; j < count2; j++)
		{
			triangleIndices.Array[count5++] = (ushort)(count3 + array2[j]);
		}
	}

	public static int DamageItem(int value, int damageCount)
	{
		int num = Terrain.ExtractContents(value);
		Block block = Blocks[num];
		if (block.Durability >= 0)
		{
			int num2 = block.GetDamage(value) + damageCount;
			if (num2 <= block.Durability)
			{
				return block.SetDamage(value, num2);
			}
			return block.GetDamageDestructionValue(value);
		}
		return value;
	}

	private static void LoadBlocksData(string data)
	{
		Dictionary<Block, bool> dictionary = new Dictionary<Block, bool>();
		data = data.Replace("\r", string.Empty);
		string[] array = data.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		string[] array2 = null;
		for (int i = 0; i < array.Length; i++)
		{
			string[] array3 = array[i].Split(new char[1] { ';' });
			if (i == 0)
			{
				array2 = new string[array3.Length - 1];
				Array.Copy(array3, 1, array2, 0, array3.Length - 1);
				continue;
			}
			if (array3.Length != array2.Length + 1)
			{
				throw new InvalidOperationException(string.Format("Not enough field values for block \"{0}\".", (array3.Length != 0) ? array3[0] : "unknown"));
			}
			string typeName = array3[0];
			if (string.IsNullOrEmpty(typeName))
			{
				continue;
			}
			Block block = m_blocks.FirstOrDefault((Block v) => v.GetType().Name == typeName);
			if (block == null)
			{
				throw new InvalidOperationException($"Block \"{typeName}\" not found when loading block data.");
			}
			if (dictionary.ContainsKey(block))
			{
				throw new InvalidOperationException($"Data for block \"{typeName}\" specified more than once when loading block data.");
			}
			dictionary.Add(block, value: true);
			Dictionary<string, FieldInfo> dictionary2 = new Dictionary<string, FieldInfo>();
			foreach (FieldInfo runtimeField in block.GetType().GetRuntimeFields())
			{
				if (runtimeField.IsPublic && !runtimeField.IsStatic)
				{
					dictionary2.Add(runtimeField.Name, runtimeField);
				}
			}
			for (int num = 1; num < array3.Length; num++)
			{
				string text = array2[num - 1];
				string text2 = array3[num];
				if (string.IsNullOrEmpty(text2))
				{
					continue;
				}
				if (!dictionary2.TryGetValue(text, out var value))
				{
					throw new InvalidOperationException($"Field \"{text}\" not found or not accessible when loading block data.");
				}
				object obj = null;
				if (text2.StartsWith("#"))
				{
					string refTypeName = text2.Substring(1);
					obj = ((!string.IsNullOrEmpty(refTypeName)) ? ((object)(m_blocks.FirstOrDefault((Block v) => v.GetType().Name == refTypeName) ?? throw new InvalidOperationException($"Reference block \"{refTypeName}\" not found when loading block data.")).BlockIndex) : ((object)block.BlockIndex));
				}
				else
				{
					obj = HumanReadableConverter.ConvertFromString(value.FieldType, text2);
				}
				value.SetValue(block, obj);
			}
		}
		using IEnumerator<Block> enumerator2 = m_blocks.Except(dictionary.Keys).GetEnumerator();
		if (enumerator2.MoveNext())
		{
			Block current2 = enumerator2.Current;
			throw new InvalidOperationException($"Data for block \"{current2.GetType().Name}\" not found when loading blocks data.");
		}
	}

	private static void CalculateSlotTexCoordTables()
	{
		for (int i = 0; i < 256; i++)
		{
			m_slotTexCoords[i] = TextureSlotToTextureCoords(i);
		}
	}

	private static Vector4 TextureSlotToTextureCoords(int slot)
	{
		int num = slot % 16;
		int num2 = slot / 16;
		float x = ((float)num + 0.001f) / 16f;
		float y = ((float)num2 + 0.001f) / 16f;
		float z = ((float)(num + 1) - 0.001f) / 16f;
		float w = ((float)(num2 + 1) - 0.001f) / 16f;
		return new Vector4(x, y, z, w);
	}

	private static BlockMesh GetImageExtrusionBlockMesh(Image image, int slot)
	{
		ImageExtrusionKey key = new ImageExtrusionKey
		{
			Image = image,
			Slot = slot
		};
		if (!m_imageExtrusionsCache.TryGetValue(key, out var value))
		{
			value = new BlockMesh();
			int num = (int)MathUtils.Round(m_slotTexCoords[slot].X * (float)image.Width);
			int num2 = (int)MathUtils.Round(m_slotTexCoords[slot].Y * (float)image.Height);
			int num3 = (int)MathUtils.Round(m_slotTexCoords[slot].Z * (float)image.Width);
			int num4 = (int)MathUtils.Round(m_slotTexCoords[slot].W * (float)image.Height);
			int num5 = MathUtils.Max(num3 - num, num4 - num2);
			value.AppendImageExtrusion(image, new Rectangle(num, num2, num3 - num, num4 - num2), new Vector3(1f / (float)num5, 1f / (float)num5, 1f / 12f), Color.White, 0);
			m_imageExtrusionsCache.Add(key, value);
		}
		return value;
	}
}
