using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public class BlockMesh
{
	private struct InternalVertex
	{
		public Vector3 Position;

		public Vector3 Normal;

		public Vector2 TextureCoordinate;
	}

	public DynamicArray<BlockMeshVertex> Vertices = new DynamicArray<BlockMeshVertex>();

	public DynamicArray<ushort> Indices = new DynamicArray<ushort>();

	public DynamicArray<sbyte> Sides;

	public BoundingBox CalculateBoundingBox()
	{
		return new BoundingBox(Vertices.Select((BlockMeshVertex v) => v.Position));
	}

	public BoundingBox CalculateBoundingBox(Matrix matrix)
	{
		return new BoundingBox(Vertices.Select((BlockMeshVertex v) => Vector3.Transform(v.Position, matrix)));
	}

	public static Matrix GetBoneAbsoluteTransform(ModelBone modelBone)
	{
		if (modelBone.ParentBone != null)
		{
			return GetBoneAbsoluteTransform(modelBone.ParentBone) * modelBone.Transform;
		}
		return modelBone.Transform;
	}

	public void AppendImageExtrusion(Image image, Rectangle bounds, Vector3 scale, Color color, int alphaThreshold)
	{
		int count = Vertices.Count;
		AppendImageExtrusionSlice(image, bounds, new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 0f), color, alphaThreshold);
		AppendImageExtrusionSlice(image, bounds, new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f), color, alphaThreshold);
		for (int i = bounds.Left; i < bounds.Right; i++)
		{
			Image image2 = new Image(1, bounds.Height);
			for (int j = bounds.Top; j < bounds.Bottom; j++)
			{
				if (i == bounds.Left || image.Pixels[i - 1 + j * image.Width].A <= alphaThreshold)
				{
					image2.Pixels[j - bounds.Top] = image.Pixels[i + j * image.Width];
				}
			}
			AppendImageExtrusionSlice(image2, new Rectangle(0, 0, image2.Width, image2.Height), new Vector3(0f, 0f, 1f), new Vector3(0f, 1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(i, bounds.Top, 0f), color, alphaThreshold);
		}
		for (int k = bounds.Left; k < bounds.Right; k++)
		{
			Image image3 = new Image(1, bounds.Height);
			for (int l = bounds.Top; l < bounds.Bottom; l++)
			{
				if (k == bounds.Right - 1 || image.Pixels[k + 1 + l * image.Width].A <= alphaThreshold)
				{
					image3.Pixels[l - bounds.Top] = image.Pixels[k + l * image.Width];
				}
			}
			AppendImageExtrusionSlice(image3, new Rectangle(0, 0, image3.Width, image3.Height), new Vector3(0f, 0f, 1f), new Vector3(0f, 1f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(k + 1, bounds.Top, 0f), color, alphaThreshold);
		}
		for (int m = bounds.Top; m < bounds.Bottom; m++)
		{
			Image image4 = new Image(bounds.Width, 1);
			for (int n = bounds.Left; n < bounds.Right; n++)
			{
				if (m == bounds.Top || image.Pixels[n + (m - 1) * image.Width].A <= alphaThreshold)
				{
					image4.Pixels[n - bounds.Left] = image.Pixels[n + m * image.Width];
				}
			}
			AppendImageExtrusionSlice(image4, new Rectangle(0, 0, image4.Width, image4.Height), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 1f, 0f), new Vector3(bounds.Left, m, 0f), color, alphaThreshold);
		}
		for (int num = bounds.Top; num < bounds.Bottom; num++)
		{
			Image image5 = new Image(bounds.Width, 1);
			for (int num2 = bounds.Left; num2 < bounds.Right; num2++)
			{
				if (num == bounds.Bottom - 1 || image.Pixels[num2 + (num + 1) * image.Width].A <= alphaThreshold)
				{
					image5.Pixels[num2 - bounds.Left] = image.Pixels[num2 + num * image.Width];
				}
			}
			AppendImageExtrusionSlice(image5, new Rectangle(0, 0, image5.Width, image5.Height), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, -1f, 0f), new Vector3(bounds.Left, num + 1, 0f), color, alphaThreshold);
		}
		for (int num3 = count; num3 < Vertices.Count; num3++)
		{
			Vertices.Array[num3].Position.X -= (float)(bounds.Left + bounds.Right) / 2f;
			Vertices.Array[num3].Position.Y -= (float)(bounds.Top + bounds.Bottom) / 2f;
			Vertices.Array[num3].Position.Z -= 0.5f;
			Vertices.Array[num3].Position.X *= scale.X;
			Vertices.Array[num3].Position.Y *= 0f - scale.Y;
			Vertices.Array[num3].Position.Z *= scale.Z;
			Vertices.Array[num3].TextureCoordinates.X /= image.Width;
			Vertices.Array[num3].TextureCoordinates.Y /= image.Height;
			Vertices.Array[num3].Color *= color;
		}
	}

	public void AppendModelMeshPart(ModelMeshPart meshPart, Matrix matrix, bool makeEmissive, bool flipWindingOrder, bool doubleSided, bool flipNormals, Color color)
	{
		VertexBuffer vertexBuffer = meshPart.VertexBuffer;
		IndexBuffer indexBuffer = meshPart.IndexBuffer;
		ReadOnlyList<VertexElement> vertexElements = vertexBuffer.VertexDeclaration.VertexElements;
		if (vertexElements.Count != 3 || vertexElements[0].Offset != 0 || vertexElements[0].Semantic != VertexElementSemantic.Position.GetSemanticString() || vertexElements[1].Offset != 12 || vertexElements[1].Semantic != VertexElementSemantic.Normal.GetSemanticString() || vertexElements[2].Offset != 24 || vertexElements[2].Semantic != VertexElementSemantic.TextureCoordinate.GetSemanticString())
		{
			throw new InvalidOperationException("Wrong vertex format for a block mesh.");
		}
		InternalVertex[] vertexData = GetVertexData<InternalVertex>(vertexBuffer);
		ushort[] indexData = GetIndexData<ushort>(indexBuffer);
		Dictionary<ushort, ushort> dictionary = new Dictionary<ushort, ushort>();
		for (int i = meshPart.StartIndex; i < meshPart.StartIndex + meshPart.IndicesCount; i++)
		{
			ushort num = indexData[i];
			if (!dictionary.ContainsKey(num))
			{
				dictionary.Add(num, (ushort)Vertices.Count);
				BlockMeshVertex item = new BlockMeshVertex
				{
					Position = Vector3.Transform(vertexData[num].Position, matrix),
					TextureCoordinates = vertexData[num].TextureCoordinate
				};
				Vector3 vector = Vector3.Normalize(Vector3.TransformNormal(flipNormals ? (-vertexData[num].Normal) : vertexData[num].Normal, matrix));
				if (makeEmissive)
				{
					item.IsEmissive = true;
					item.Color = color;
				}
				else
				{
					item.Color = color * LightingManager.CalculateLighting(vector);
					item.Color.A = color.A;
				}
				item.Face = (byte)CellFace.Vector3ToFace(vector);
				Vertices.Add(item);
			}
		}
		for (int j = 0; j < meshPart.IndicesCount / 3; j++)
		{
			if (doubleSided)
			{
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 1]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 2]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 2]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 1]]);
			}
			else if (flipWindingOrder)
			{
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 2]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 1]]);
			}
			else
			{
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 1]]);
				Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 2]]);
			}
		}
		Trim();
	}

	public void AppendBlockMesh(BlockMesh blockMesh)
	{
		int count = Vertices.Count;
		for (int i = 0; i < blockMesh.Vertices.Count; i++)
		{
			Vertices.Add(blockMesh.Vertices.Array[i]);
		}
		for (int j = 0; j < blockMesh.Indices.Count; j++)
		{
			Indices.Add((ushort)(blockMesh.Indices.Array[j] + count));
		}
		Trim();
	}

	public void BlendBlockMesh(BlockMesh blockMesh, float factor)
	{
		if (blockMesh.Vertices.Count != Vertices.Count)
		{
			throw new InvalidOperationException("Meshes do not match.");
		}
		for (int i = 0; i < Vertices.Count; i++)
		{
			Vector3 position = Vertices.Array[i].Position;
			Vector3 position2 = blockMesh.Vertices.Array[i].Position;
			Vertices.Array[i].Position = Vector3.Lerp(position, position2, factor);
		}
	}

	public void TransformPositions(Matrix matrix, int facesMask = -1)
	{
		for (int i = 0; i < Vertices.Count; i++)
		{
			if (((1 << (int)Vertices.Array[i].Face) & facesMask) != 0)
			{
				Vertices.Array[i].Position = Vector3.Transform(Vertices.Array[i].Position, matrix);
			}
		}
	}

	public void TransformTextureCoordinates(Matrix matrix, int facesMask = -1)
	{
		for (int i = 0; i < Vertices.Count; i++)
		{
			if (((1 << (int)Vertices.Array[i].Face) & facesMask) != 0)
			{
				Vertices.Array[i].TextureCoordinates = Vector2.Transform(Vertices.Array[i].TextureCoordinates, matrix);
			}
		}
	}

	public void SetColor(Color color, int facesMask = -1)
	{
		for (int i = 0; i < Vertices.Count; i++)
		{
			if (((1 << (int)Vertices.Array[i].Face) & facesMask) != 0)
			{
				Vertices.Array[i].Color = color;
			}
		}
	}

	public void ModulateColor(Color color, int facesMask = -1)
	{
		for (int i = 0; i < Vertices.Count; i++)
		{
			if (((1 << (int)Vertices.Array[i].Face) & facesMask) != 0)
			{
				Vertices.Array[i].Color *= color;
			}
		}
	}

	public void GenerateSidesData()
	{
		Sides = new DynamicArray<sbyte>();
		Sides.Count = Indices.Count / 3;
		for (int i = 0; i < Sides.Count; i++)
		{
			int num = Indices.Array[3 * i];
			int num2 = Indices.Array[3 * i + 1];
			int num3 = Indices.Array[3 * i + 2];
			Vector3 position = Vertices.Array[num].Position;
			Vector3 position2 = Vertices.Array[num2].Position;
			Vector3 position3 = Vertices.Array[num3].Position;
			if (IsNear(position.Z, position2.Z, position3.Z, 1f))
			{
				Sides.Array[i] = 0;
			}
			else if (IsNear(position.X, position2.X, position3.X, 1f))
			{
				Sides.Array[i] = 1;
			}
			else if (IsNear(position.Z, position2.Z, position3.Z, 0f))
			{
				Sides.Array[i] = 2;
			}
			else if (IsNear(position.X, position2.X, position3.X, 0f))
			{
				Sides.Array[i] = 3;
			}
			else if (IsNear(position.Y, position2.Y, position3.Y, 1f))
			{
				Sides.Array[i] = 4;
			}
			else if (IsNear(position.Y, position2.Y, position3.Y, 0f))
			{
				Sides.Array[i] = 5;
			}
			else
			{
				Sides.Array[i] = -1;
			}
		}
	}

	public void Trim()
	{
		Vertices.Capacity = Vertices.Count;
		Indices.Capacity = Indices.Count;
		if (Sides != null)
		{
			Sides.Capacity = Sides.Count;
		}
	}

	public static T[] GetVertexData<T>(VertexBuffer vertexBuffer)
	{
		if (!(vertexBuffer.Tag is byte[] array))
		{
			throw new InvalidOperationException("VertexBuffer does not contain source data in Tag.");
		}
		if (array.Length % Utilities.SizeOf<T>() != 0)
		{
			throw new InvalidOperationException("VertexBuffer data size is not a whole multiply of target type size.");
		}
		T[] array2 = new T[array.Length / Utilities.SizeOf<T>()];
		GCHandle gCHandle = GCHandle.Alloc(array2, GCHandleType.Pinned);
		try
		{
			Marshal.Copy(array, 0, gCHandle.AddrOfPinnedObject(), Utilities.SizeOf<T>() * array2.Length);
			return array2;
		}
		finally
		{
			gCHandle.Free();
		}
	}

	public static T[] GetIndexData<T>(IndexBuffer indexBuffer)
	{
		if (!(indexBuffer.Tag is byte[] array))
		{
			throw new InvalidOperationException("IndexBuffer does not contain source data in Tag.");
		}
		if (array.Length % Utilities.SizeOf<T>() != 0)
		{
			throw new InvalidOperationException("IndexBuffer data size is not a whole multiply of target type size.");
		}
		T[] array2 = new T[array.Length / Utilities.SizeOf<T>()];
		GCHandle gCHandle = GCHandle.Alloc(array2, GCHandleType.Pinned);
		try
		{
			Marshal.Copy(array, 0, gCHandle.AddrOfPinnedObject(), Utilities.SizeOf<T>() * array2.Length);
			return array2;
		}
		finally
		{
			gCHandle.Free();
		}
	}

	private static bool IsNear(float v1, float v2, float v3, float t)
	{
		if (v1 - t >= -0.001f && v1 - t <= 0.001f && v2 - t >= -0.001f && v2 - t <= 0.001f && v3 - t >= -0.001f)
		{
			return v3 - t <= 0.001f;
		}
		return false;
	}

	private void AppendImageExtrusionSlice(Image slice, Rectangle bounds, Vector3 right, Vector3 up, Vector3 forward, Vector3 position, Color color, int alphaThreshold)
	{
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MinValue;
		int num4 = int.MinValue;
		for (int i = bounds.Top; i < bounds.Bottom; i++)
		{
			for (int j = bounds.Left; j < bounds.Right; j++)
			{
				if (slice.Pixels[j + i * slice.Width].A > alphaThreshold)
				{
					num = MathUtils.Min(num, j);
					num2 = MathUtils.Min(num2, i);
					num3 = MathUtils.Max(num3, j);
					num4 = MathUtils.Max(num4, i);
				}
			}
		}
		if (num != int.MaxValue)
		{
			Matrix m = new Matrix(right.X, right.Y, right.Z, 0f, up.X, up.Y, up.Z, 0f, forward.X, forward.Y, forward.Z, 0f, position.X, position.Y, position.Z, 1f);
			bool flip = m.Determinant() > 0f;
			float s = LightingManager.CalculateLighting(-forward);
			Vector3 p = Vector3.Transform(new Vector3(num, num2, 0f), m);
			Vector3 p2 = Vector3.Transform(new Vector3(num3 + 1, num2, 0f), m);
			Vector3 p3 = Vector3.Transform(new Vector3(num, num4 + 1, 0f), m);
			Vector3 p4 = Vector3.Transform(new Vector3(num3 + 1, num4 + 1, 0f), m);
			AppendImageExtrusionRectangle(p, p2, p3, p4, forward, flip, Color.MultiplyColorOnly(color, s));
		}
	}

	private void AppendImageExtrusionRectangle(Vector3 p11, Vector3 p21, Vector3 p12, Vector3 p22, Vector3 forward, bool flip, Color color)
	{
		int count = Vertices.Count;
		Vertices.Count += 4;
		Vertices[Vertices.Count - 4] = new BlockMeshVertex
		{
			Position = p11,
			TextureCoordinates = p11.XY + forward.XY / 2f,
			Color = color
		};
		Vertices[Vertices.Count - 3] = new BlockMeshVertex
		{
			Position = p21,
			TextureCoordinates = p21.XY + forward.XY / 2f,
			Color = color
		};
		Vertices[Vertices.Count - 2] = new BlockMeshVertex
		{
			Position = p12,
			TextureCoordinates = p12.XY + forward.XY / 2f,
			Color = color
		};
		Vertices[Vertices.Count - 1] = new BlockMeshVertex
		{
			Position = p22,
			TextureCoordinates = p22.XY + forward.XY / 2f,
			Color = color
		};
		Indices.Count += 6;
		if (flip)
		{
			Indices[Indices.Count - 6] = (ushort)count;
			Indices[Indices.Count - 5] = (ushort)(count + 2);
			Indices[Indices.Count - 4] = (ushort)(count + 1);
			Indices[Indices.Count - 3] = (ushort)(count + 2);
			Indices[Indices.Count - 2] = (ushort)(count + 3);
			Indices[Indices.Count - 1] = (ushort)(count + 1);
		}
		else
		{
			Indices[Indices.Count - 6] = (ushort)count;
			Indices[Indices.Count - 5] = (ushort)(count + 1);
			Indices[Indices.Count - 4] = (ushort)(count + 2);
			Indices[Indices.Count - 3] = (ushort)(count + 2);
			Indices[Indices.Count - 2] = (ushort)(count + 1);
			Indices[Indices.Count - 1] = (ushort)(count + 3);
		}
	}
}
