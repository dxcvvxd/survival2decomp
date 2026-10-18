using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Engine.Graphics;

namespace Engine.Media;

public static class Collada
{
	private struct Vertex : IEquatable<Vertex>
	{
		public byte[] Data;

		public int Start;

		public int Count;

		private int m_hashCode;

		public Vertex(byte[] data, int start, int count)
		{
			Data = data;
			Start = start;
			Count = count;
			m_hashCode = 0;
			for (int i = 0; i < Count; i++)
			{
				m_hashCode += (7919 * i + 977) * Data[i + Start];
			}
		}

		public bool Equals(Vertex other)
		{
			if (m_hashCode != other.m_hashCode || Data.Length != other.Data.Length)
			{
				return false;
			}
			for (int i = 0; i < Count; i++)
			{
				if (Data[i + Start] != other.Data[i + other.Start])
				{
					return false;
				}
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Color))
			{
				return false;
			}
			return Equals((Color)obj);
		}

		public override int GetHashCode()
		{
			return m_hashCode;
		}
	}

	private class ColladaRoot
	{
		public static readonly XNamespace Namespace = "http://www.collada.org/2005/11/COLLADASchema";

		public readonly Dictionary<string, ColladaNameId> ObjectsById = new Dictionary<string, ColladaNameId>();

		public ColladaAsset Asset;

		public readonly List<ColladaLibraryGeometries> LibraryGeometries = new List<ColladaLibraryGeometries>();

		public readonly List<ColladaLibraryVisualScenes> LibraryVisualScenes = new List<ColladaLibraryVisualScenes>();

		public ColladaScene Scene;

		public ColladaRoot(ModelData modelData)
		{
			Asset = new ColladaAsset();
			LibraryGeometries.Add(new ColladaLibraryGeometries());
			foreach (ModelMeshData mesh in modelData.Meshes)
			{
				foreach (ModelMeshPartData meshPart in mesh.MeshParts)
				{
					LibraryGeometries[0].Geometries.Add(new ColladaGeometry(this, modelData, mesh, meshPart));
				}
			}
			LibraryVisualScenes.Add(new ColladaLibraryVisualScenes());
			ColladaVisualScene colladaVisualScene = new ColladaVisualScene(this)
			{
				ChildNodes = 
				{
					new ColladaNode(this, modelData, modelData.Bones[0])
				}
			};
			LibraryVisualScenes[0].VisualScenes.Add(colladaVisualScene);
			Scene = new ColladaScene
			{
				VisualScene = colladaVisualScene
			};
		}

		public ColladaRoot(XElement node)
		{
			Asset = new ColladaAsset(node.Element(Namespace + "asset"));
			foreach (XElement item in node.Elements(Namespace + "library_geometries"))
			{
				LibraryGeometries.Add(new ColladaLibraryGeometries(this, item));
			}
			foreach (XElement item2 in node.Elements(Namespace + "library_visual_scenes"))
			{
				LibraryVisualScenes.Add(new ColladaLibraryVisualScenes(this, item2));
			}
			Scene = new ColladaScene(this, node.Element(Namespace + "scene"));
		}

		public void Save(ModelData modelData)
		{
			if (Scene.VisualScene.ChildNodes.Count > 1)
			{
				ModelBoneData modelBoneData = new ModelBoneData();
				modelData.Bones.Add(modelBoneData);
				modelBoneData.ParentBoneIndex = -1;
				modelBoneData.Name = "EngineRoot";
				modelBoneData.Transform = Matrix.Identity;
				foreach (ColladaNode childNode in Scene.VisualScene.ChildNodes)
				{
					childNode.Save(modelData, modelBoneData, Matrix.CreateScale(Asset.Meter), new ModelBoneData());
				}
			}
			else
			{
				foreach (ColladaNode childNode2 in Scene.VisualScene.ChildNodes)
				{
					childNode2.Save(modelData, null, Matrix.CreateScale(Asset.Meter), new ModelBoneData());
				}
			}
			foreach (ModelBuffersData buffer in modelData.Buffers)
			{
				IndexVertices(buffer.VertexDeclaration.VertexStride, buffer.Vertices, out buffer.Vertices, out buffer.Indices);
			}
		}

		public void Save(XElement node)
		{
			node.SetAttributeValue("version", "1.4.1");
			Asset.Save(CreateElement(node, Namespace + "asset"));
			foreach (ColladaLibraryGeometries libraryGeometry in LibraryGeometries)
			{
				libraryGeometry.Save(CreateElement(node, Namespace + "library_geometries"));
			}
			foreach (ColladaLibraryVisualScenes libraryVisualScene in LibraryVisualScenes)
			{
				libraryVisualScene.Save(CreateElement(node, Namespace + "library_visual_scenes"));
			}
			Scene.Save(CreateElement(node, Namespace + "scene"));
		}
	}

	private class ColladaNameId
	{
		public string Id;

		public string Name;

		public ColladaNameId(ColladaRoot colladaRoot, string id, string name)
		{
			Id = id;
			Name = name;
			colladaRoot.ObjectsById.Add(Id, this);
		}

		public ColladaNameId(ColladaRoot colladaRoot, string id)
			: this(colladaRoot, id, id)
		{
		}

		public ColladaNameId(ColladaRoot colladaRoot, XElement node, string idPostfix = "")
		{
			XAttribute xAttribute = node.Attribute("id");
			if (xAttribute != null)
			{
				Id = xAttribute.Value + idPostfix;
				colladaRoot.ObjectsById.Add(Id, this);
			}
			XAttribute xAttribute2 = node.Attribute("name");
			if (xAttribute2 != null)
			{
				Name = xAttribute2.Value;
			}
		}

		public virtual void Save(XElement node)
		{
			node.SetAttributeValue("id", Id);
			node.SetAttributeValue("name", Name);
		}
	}

	private class ColladaAsset
	{
		public float Meter = 1f;

		public ColladaAsset()
		{
		}

		public ColladaAsset(XElement node)
		{
			XElement xElement = node.Element(ColladaRoot.Namespace + "unit");
			if (xElement != null)
			{
				XAttribute xAttribute = xElement.Attribute("meter");
				if (xAttribute != null)
				{
					Meter = float.Parse(xAttribute.Value, CultureInfo.InvariantCulture);
				}
			}
		}

		public void Save(XElement node)
		{
			XElement xElement = CreateElement(node, ColladaRoot.Namespace + "unit");
			xElement.SetAttributeValue("name", "meter");
			xElement.SetAttributeValue("meter", Meter.ToString(CultureInfo.InvariantCulture));
		}
	}

	private class ColladaLibraryVisualScenes
	{
		public readonly List<ColladaVisualScene> VisualScenes = new List<ColladaVisualScene>();

		public ColladaLibraryVisualScenes()
		{
		}

		public ColladaLibraryVisualScenes(ColladaRoot colladaRoot, XElement node)
		{
			foreach (XElement item in node.Elements(ColladaRoot.Namespace + "visual_scene"))
			{
				VisualScenes.Add(new ColladaVisualScene(colladaRoot, item));
			}
		}

		public void Save(XElement node)
		{
			foreach (ColladaVisualScene visualScene in VisualScenes)
			{
				visualScene.Save(CreateElement(node, ColladaRoot.Namespace + "visual_scene"));
			}
		}
	}

	private class ColladaLibraryGeometries
	{
		public readonly List<ColladaGeometry> Geometries = new List<ColladaGeometry>();

		public ColladaLibraryGeometries()
		{
		}

		public ColladaLibraryGeometries(ColladaRoot colladaRoot, XElement node)
		{
			foreach (XElement item in node.Elements(ColladaRoot.Namespace + "geometry"))
			{
				Geometries.Add(new ColladaGeometry(colladaRoot, item));
			}
		}

		public void Save(XElement node)
		{
			foreach (ColladaGeometry geometry in Geometries)
			{
				geometry.Save(CreateElement(node, ColladaRoot.Namespace + "geometry"));
			}
		}
	}

	private class ColladaScene
	{
		public ColladaVisualScene VisualScene;

		public ColladaScene()
		{
		}

		public ColladaScene(ColladaRoot colladaRoot, XElement node)
		{
			XElement xElement = node.Element(ColladaRoot.Namespace + "instance_visual_scene");
			VisualScene = (ColladaVisualScene)colladaRoot.ObjectsById[xElement.Attribute("url").Value.Substring(1) + "-ColladaVisualScene"];
		}

		public void Save(XElement node)
		{
			CreateElement(node, ColladaRoot.Namespace + "instance_visual_scene").SetAttributeValue("url", "#" + VisualScene.Id);
		}
	}

	private class ColladaVisualScene : ColladaNameId
	{
		public readonly List<ColladaNode> ChildNodes = new List<ColladaNode>();

		public ColladaVisualScene(ColladaRoot colladaRoot)
			: base(colladaRoot, "Scene")
		{
		}

		public ColladaVisualScene(ColladaRoot colladaRoot, XElement node)
			: base(colladaRoot, node, "-ColladaVisualScene")
		{
			foreach (XElement item in node.Elements(ColladaRoot.Namespace + "node"))
			{
				ChildNodes.Add(new ColladaNode(colladaRoot, item));
			}
		}

		public override void Save(XElement node)
		{
			base.Save(node);
			foreach (ColladaNode childNode in ChildNodes)
			{
				childNode.Save(CreateElement(node, ColladaRoot.Namespace + "node"));
			}
		}
	}

	private class ColladaNode : ColladaNameId
	{
		public Matrix Transform = Matrix.Identity;

		public readonly List<ColladaNode> Children = new List<ColladaNode>();

		public readonly List<ColladaGeometry> Geometries = new List<ColladaGeometry>();

		public ColladaNode(ColladaRoot colladaRoot, ModelData modelData, ModelBoneData modelBoneData)
			: base(colladaRoot, modelBoneData.Name)
		{
			Transform = modelBoneData.Transform;
			int index = modelData.Bones.IndexOf(modelBoneData);
			foreach (ModelBoneData item in modelData.Bones.Where((ModelBoneData b) => b.ParentBoneIndex == index))
			{
				Children.Add(new ColladaNode(colladaRoot, modelData, item));
			}
			foreach (ModelMeshData item2 in modelData.Meshes.Where((ModelMeshData m) => m.ParentBoneIndex == index))
			{
				foreach (ModelMeshPartData meshPart in item2.MeshParts)
				{
					string key = ColladaGeometry.CreateId(item2, meshPart);
					Geometries.Add((ColladaGeometry)colladaRoot.ObjectsById[key]);
				}
			}
		}

		public ColladaNode(ColladaRoot colladaRoot, XElement node)
			: base(colladaRoot, node)
		{
			foreach (XElement item in node.Elements())
			{
				if (item.Name == ColladaRoot.Namespace + "matrix")
				{
					float[] array = (from s in item.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
						select float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
					Transform = Matrix.Transpose(new Matrix(array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7], array[8], array[9], array[10], array[11], array[12], array[13], array[14], array[15])) * Transform;
				}
				else if (item.Name == ColladaRoot.Namespace + "translate")
				{
					float[] array2 = (from s in item.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
						select float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
					Transform = Matrix.CreateTranslation(array2[0], array2[1], array2[2]) * Transform;
				}
				else if (item.Name == ColladaRoot.Namespace + "rotate")
				{
					float[] array3 = (from s in item.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
						select float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
					Transform = Matrix.CreateFromAxisAngle(new Vector3(array3[0], array3[1], array3[2]), MathUtils.DegToRad(array3[3])) * Transform;
				}
				else if (item.Name == ColladaRoot.Namespace + "scale")
				{
					float[] array4 = (from s in item.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
						select float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
					Transform = Matrix.CreateScale(array4[0], array4[1], array4[2]) * Transform;
				}
			}
			foreach (XElement item2 in node.Elements(ColladaRoot.Namespace + "node"))
			{
				Children.Add(new ColladaNode(colladaRoot, item2));
			}
			foreach (XElement item3 in node.Elements(ColladaRoot.Namespace + "instance_geometry"))
			{
				Geometries.Add((ColladaGeometry)colladaRoot.ObjectsById[item3.Attribute("url").Value.Substring(1)]);
			}
		}

		public void Save(ModelData modelData, ModelBoneData parentModelBoneData, Matrix transform, ModelBoneData modelBoneData)
		{
			modelData.Bones.Add(modelBoneData);
			modelBoneData.ParentBoneIndex = ((parentModelBoneData != null) ? modelData.Bones.IndexOf(parentModelBoneData) : (-1));
			modelBoneData.Name = Name;
			modelBoneData.Transform = Transform * transform;
			foreach (ColladaNode child in Children)
			{
				child.Save(modelData, modelBoneData, Matrix.Identity, new ModelBoneData());
			}
			foreach (ColladaGeometry geometry in Geometries)
			{
				geometry.Mesh.Save(modelData, modelBoneData, new ModelMeshData());
			}
		}

		public override void Save(XElement node)
		{
			base.Save(node);
			XElement xElement = CreateElement(node, ColladaRoot.Namespace + "matrix");
			Matrix matrix = Matrix.Transpose(Transform);
			xElement.Value = string.Join(" ", new object[16]
			{
				matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32,
				matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44
			});
			foreach (ColladaNode child in Children)
			{
				child.Save(CreateElement(node, ColladaRoot.Namespace + "node"));
			}
			foreach (ColladaGeometry geometry in Geometries)
			{
				XElement xElement2 = CreateElement(node, ColladaRoot.Namespace + "instance_geometry");
				xElement2.SetAttributeValue("url", "#" + geometry.Id);
				xElement2.SetAttributeValue("name", Name);
			}
		}
	}

	private class ColladaGeometry : ColladaNameId
	{
		public ColladaMesh Mesh;

		public static string CreateId(ModelMeshData modelMeshData, ModelMeshPartData modelMeshPartData)
		{
			return modelMeshData.Name + "-part" + modelMeshData.MeshParts.IndexOf(modelMeshPartData).ToString(CultureInfo.InvariantCulture);
		}

		public ColladaGeometry(ColladaRoot colladaRoot, ModelData modelData, ModelMeshData modelMeshData, ModelMeshPartData modelMeshPartData)
			: base(colladaRoot, CreateId(modelMeshData, modelMeshPartData), null)
		{
			Mesh = new ColladaMesh(colladaRoot, this, modelData, modelMeshPartData);
		}

		public ColladaGeometry(ColladaRoot colladaRoot, XElement node)
			: base(colladaRoot, node)
		{
			XElement xElement = node.Element(ColladaRoot.Namespace + "mesh");
			if (xElement != null)
			{
				Mesh = new ColladaMesh(colladaRoot, xElement);
			}
		}

		public override void Save(XElement node)
		{
			base.Save(node);
			if (Mesh != null)
			{
				Mesh.Save(CreateElement(node, ColladaRoot.Namespace + "mesh"));
			}
		}
	}

	private class ColladaMesh
	{
		public readonly List<ColladaSource> Sources = new List<ColladaSource>();

		public ColladaVertices Vertices;

		public readonly List<ColladaPolygons> Polygons = new List<ColladaPolygons>();

		public unsafe ColladaMesh(ColladaRoot colladaRoot, ColladaGeometry colladaGeometry, ModelData modelData, ModelMeshPartData modelMeshPartData)
		{
			Polygons.Add(new ColladaPolygons());
			ModelBuffersData modelBuffersData = modelData.Buffers[modelMeshPartData.BuffersDataIndex];
			VertexDeclaration vertexDeclaration = modelBuffersData.VertexDeclaration;
			ReadOnlyList<VertexElement> vertexElements = vertexDeclaration.VertexElements;
			fixed (byte* ptr = &modelBuffersData.Indices[0])
			{
				fixed (byte* ptr2 = &modelBuffersData.Vertices[0])
				{
					Dictionary<ushort, ushort> dictionary = new Dictionary<ushort, ushort>();
					for (int i = 0; i < modelMeshPartData.IndicesCount; i++)
					{
						int num = ((i % 3 == 0) ? i : ((i % 3 != 1) ? (i - 1) : (i + 1)));
						ushort key = ((ushort*)ptr)[num + modelMeshPartData.StartIndex];
						if (!dictionary.TryGetValue(key, out var value))
						{
							value = (ushort)dictionary.Count;
							dictionary.Add(key, value);
						}
						for (int j = 0; j < vertexElements.Count; j++)
						{
							Polygons[0].P.Add(value);
						}
					}
					foreach (VertexElement item in vertexElements)
					{
						ColladaSource colladaSource = new ColladaSource(colladaRoot, colladaGeometry.Id + "-" + item.Semantic);
						ColladaFloatArray colladaFloatArray = new ColladaFloatArray(colladaRoot, colladaSource.Id + "-array");
						ColladaAccessor colladaAccessor = new ColladaAccessor
						{
							Source = colladaFloatArray
						};
						Sources.Add(colladaSource);
						colladaSource.FloatArray = colladaFloatArray;
						colladaSource.Accessor = colladaAccessor;
						if (item.SemanticName == "POSITION")
						{
							colladaAccessor.Stride = 3;
							colladaFloatArray.Array = new float[3 * dictionary.Count];
							foreach (KeyValuePair<ushort, ushort> item2 in dictionary)
							{
								int num2 = item2.Key * vertexDeclaration.VertexStride + item.Offset;
								colladaFloatArray.Array[3 * item2.Value] = *(float*)(ptr2 + num2);
								colladaFloatArray.Array[3 * item2.Value + 1] = ((float*)(ptr2 + num2))[1];
								colladaFloatArray.Array[3 * item2.Value + 2] = ((float*)(ptr2 + num2))[2];
							}
							Vertices = new ColladaVertices(colladaRoot, colladaSource)
							{
								Semantic = item.SemanticName,
								Source = colladaSource
							};
						}
						else if (item.SemanticName == "NORMAL")
						{
							colladaAccessor.Stride = 3;
							colladaFloatArray.Array = new float[3 * dictionary.Count];
							foreach (KeyValuePair<ushort, ushort> item3 in dictionary)
							{
								int num3 = item3.Key * vertexDeclaration.VertexStride + item.Offset;
								colladaFloatArray.Array[3 * item3.Value] = *(float*)(ptr2 + num3);
								colladaFloatArray.Array[3 * item3.Value + 1] = ((float*)(ptr2 + num3))[1];
								colladaFloatArray.Array[3 * item3.Value + 2] = ((float*)(ptr2 + num3))[2];
							}
						}
						else if (item.SemanticName == "TEXCOORD")
						{
							colladaAccessor.Stride = 2;
							colladaFloatArray.Array = new float[2 * dictionary.Count];
							foreach (KeyValuePair<ushort, ushort> item4 in dictionary)
							{
								int num4 = item4.Key * vertexDeclaration.VertexStride + item.Offset;
								colladaFloatArray.Array[2 * item4.Value] = *(float*)(ptr2 + num4);
								colladaFloatArray.Array[2 * item4.Value + 1] = ((float*)(ptr2 + num4))[1];
							}
						}
						else if (item.SemanticName == "COLOR")
						{
							colladaAccessor.Stride = 4;
							colladaFloatArray.Array = new float[4 * dictionary.Count];
							foreach (KeyValuePair<ushort, ushort> item5 in dictionary)
							{
								int num5 = item5.Key * vertexDeclaration.VertexStride + item.Offset;
								colladaFloatArray.Array[4 * item5.Value] = (float)(int)ptr2[num5] / 255f;
								colladaFloatArray.Array[4 * item5.Value + 1] = (float)(int)(ptr2 + num5)[1] / 255f;
								colladaFloatArray.Array[4 * item5.Value + 2] = (float)(int)(ptr2 + num5)[2] / 255f;
								colladaFloatArray.Array[4 * item5.Value + 3] = (float)(int)(ptr2 + num5)[3] / 255f;
							}
						}
						Polygons[0].Inputs.Add(new ColladaInput
						{
							Semantic = ((item.SemanticName == "POSITION") ? "VERTEX" : item.SemanticName),
							Set = item.SemanticIndex,
							Offset = vertexElements.IndexOf(item),
							Source = colladaSource,
							Vertices = ((item.SemanticName == "POSITION") ? Vertices : null)
						});
					}
				}
			}
		}

		public ColladaMesh(ColladaRoot colladaRoot, XElement node)
		{
			foreach (XElement item in node.Elements(ColladaRoot.Namespace + "source"))
			{
				Sources.Add(new ColladaSource(colladaRoot, item));
			}
			XElement node2 = node.Element(ColladaRoot.Namespace + "vertices");
			Vertices = new ColladaVertices(colladaRoot, node2);
			foreach (XElement item2 in node.Elements(ColladaRoot.Namespace + "polygons").Concat(node.Elements(ColladaRoot.Namespace + "polylist")).Concat(node.Elements(ColladaRoot.Namespace + "triangles")))
			{
				Polygons.Add(new ColladaPolygons(colladaRoot, item2));
			}
		}

		public void Save(ModelData modelData, ModelBoneData parentBone, ModelMeshData modelMeshData)
		{
			modelData.Meshes.Add(modelMeshData);
			modelMeshData.Name = parentBone.Name;
			modelMeshData.ParentBoneIndex = modelData.Bones.IndexOf(parentBone);
			foreach (ColladaPolygons polygon in Polygons)
			{
				polygon.Save(modelData, modelMeshData, new ModelMeshPartData());
			}
		}

		public void Save(XElement node)
		{
			foreach (ColladaSource source in Sources)
			{
				source.Save(CreateElement(node, ColladaRoot.Namespace + "source"));
			}
			Vertices.Save(CreateElement(node, ColladaRoot.Namespace + "vertices"));
			foreach (ColladaPolygons polygon in Polygons)
			{
				polygon.Save(CreateElement(node, ColladaRoot.Namespace + "triangles"));
			}
		}
	}

	private class ColladaSource : ColladaNameId
	{
		public ColladaFloatArray FloatArray;

		public ColladaAccessor Accessor;

		public ColladaSource(ColladaRoot colladaRoot, string id)
			: base(colladaRoot, id, null)
		{
		}

		public ColladaSource(ColladaRoot colladaRoot, XElement node)
			: base(colladaRoot, node)
		{
			XElement xElement = node.Element(ColladaRoot.Namespace + "float_array");
			if (xElement != null)
			{
				FloatArray = new ColladaFloatArray(colladaRoot, xElement);
			}
			XElement xElement2 = node.Element(ColladaRoot.Namespace + "technique_common");
			if (xElement2 != null)
			{
				XElement xElement3 = xElement2.Element(ColladaRoot.Namespace + "accessor");
				if (xElement3 != null)
				{
					Accessor = new ColladaAccessor(colladaRoot, xElement3);
				}
			}
		}

		public override void Save(XElement node)
		{
			base.Save(node);
			if (FloatArray != null)
			{
				FloatArray.Save(CreateElement(node, ColladaRoot.Namespace + "float_array"));
			}
			if (Accessor != null)
			{
				XElement parent = CreateElement(node, ColladaRoot.Namespace + "technique_common");
				Accessor.Save(CreateElement(parent, ColladaRoot.Namespace + "accessor"));
			}
		}
	}

	private class ColladaFloatArray : ColladaNameId
	{
		public float[] Array = new float[0];

		public ColladaFloatArray(ColladaRoot colladaRoot, string id)
			: base(colladaRoot, id, null)
		{
		}

		public ColladaFloatArray(ColladaRoot colladaRoot, XElement node)
			: base(colladaRoot, node)
		{
			Array = (from s in node.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
				select float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
		}

		public override void Save(XElement node)
		{
			base.Save(node);
			node.SetAttributeValue("count", Array.Length);
			node.Value = string.Join(" ", Array.Select((float f) => f.ToString(CultureInfo.InvariantCulture)));
		}
	}

	private class ColladaAccessor
	{
		public ColladaFloatArray Source;

		public int Offset;

		public int Stride = 1;

		public ColladaAccessor()
		{
		}

		public ColladaAccessor(ColladaRoot colladaRoot, XElement node)
		{
			Source = (ColladaFloatArray)colladaRoot.ObjectsById[node.Attribute("source").Value.Substring(1)];
			XAttribute xAttribute = node.Attribute("offset");
			if (xAttribute != null)
			{
				Offset = int.Parse(xAttribute.Value, CultureInfo.InvariantCulture);
			}
			XAttribute xAttribute2 = node.Attribute("stride");
			if (xAttribute2 != null)
			{
				Stride = int.Parse(xAttribute2.Value, CultureInfo.InvariantCulture);
			}
		}

		public void Save(XElement node)
		{
			node.SetAttributeValue("source", "#" + Source.Id);
			node.SetAttributeValue("offset", Offset.ToString(CultureInfo.InvariantCulture));
			node.SetAttributeValue("count", (Source.Array.Length / Stride).ToString(CultureInfo.InvariantCulture));
			node.SetAttributeValue("stride", Stride.ToString(CultureInfo.InvariantCulture));
			for (int i = 0; i < Stride; i++)
			{
				CreateElement(node, ColladaRoot.Namespace + "param").SetAttributeValue("type", "float");
			}
		}
	}

	private class ColladaVertices : ColladaNameId
	{
		public string Semantic;

		public ColladaSource Source;

		public ColladaVertices(ColladaRoot colladaRoot, ColladaSource colladaSource)
			: base(colladaRoot, colladaSource.Id + "-vertices", null)
		{
		}

		public ColladaVertices(ColladaRoot colladaRoot, XElement node)
			: base(colladaRoot, node)
		{
			XElement xElement = node.Element(ColladaRoot.Namespace + "input");
			Semantic = xElement.Attribute("semantic").Value;
			Source = (ColladaSource)colladaRoot.ObjectsById[xElement.Attribute("source").Value.Substring(1)];
		}

		public override void Save(XElement node)
		{
			base.Save(node);
			XElement xElement = CreateElement(node, ColladaRoot.Namespace + "input");
			xElement.SetAttributeValue("semantic", Semantic);
			xElement.SetAttributeValue("source", "#" + Source.Id);
		}
	}

	private class ColladaPolygons
	{
		public List<ColladaInput> Inputs = new List<ColladaInput>();

		public List<int> VCount = new List<int>();

		public List<int> P = new List<int>();

		public ColladaPolygons()
		{
		}

		public ColladaPolygons(ColladaRoot colladaRoot, XElement node)
		{
			foreach (XElement item in node.Elements(ColladaRoot.Namespace + "input"))
			{
				Inputs.Add(new ColladaInput(colladaRoot, item));
			}
			foreach (XElement item2 in node.Elements(ColladaRoot.Namespace + "vcount"))
			{
				VCount.AddRange(from s in item2.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
					select int.Parse(s, CultureInfo.InvariantCulture));
			}
			foreach (XElement item3 in node.Elements(ColladaRoot.Namespace + "p"))
			{
				P.AddRange(from s in item3.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
					select int.Parse(s, CultureInfo.InvariantCulture));
			}
		}

		public void Save(ModelData modelData, ModelMeshData modelMeshData, ModelMeshPartData modelMeshPartData)
		{
			int num = 0;
			Dictionary<VertexElement, ColladaInput> dictionary = new Dictionary<VertexElement, ColladaInput>();
			foreach (ColladaInput input in Inputs)
			{
				string text = ((input.Set == 0) ? string.Empty : input.Set.ToString(CultureInfo.InvariantCulture));
				if (input.Semantic == "POSITION")
				{
					dictionary[new VertexElement(num, VertexElementFormat.Vector3, "POSITION" + text)] = input;
					num += 12;
				}
				else if (input.Semantic == "NORMAL")
				{
					dictionary[new VertexElement(num, VertexElementFormat.Vector3, "NORMAL" + text)] = input;
					num += 12;
				}
				else if (input.Semantic == "TEXCOORD")
				{
					dictionary[new VertexElement(num, VertexElementFormat.Vector2, "TEXCOORD" + text)] = input;
					num += 8;
				}
				else if (input.Semantic == "COLOR")
				{
					dictionary[new VertexElement(num, VertexElementFormat.NormalizedByte4, "COLOR" + text)] = input;
					num += 4;
				}
			}
			VertexDeclaration vertexDeclaration = new VertexDeclaration(dictionary.Keys.ToArray());
			ModelBuffersData modelBuffersData = modelData.Buffers.FirstOrDefault((ModelBuffersData vd) => vd.VertexDeclaration == vertexDeclaration);
			if (modelBuffersData == null)
			{
				modelBuffersData = new ModelBuffersData();
				modelData.Buffers.Add(modelBuffersData);
				modelBuffersData.VertexDeclaration = vertexDeclaration;
			}
			modelMeshPartData.BuffersDataIndex = modelData.Buffers.IndexOf(modelBuffersData);
			int num2 = P.Count / Inputs.Count;
			List<int> list = new List<int>();
			if (VCount.Count == 0)
			{
				int num3 = 0;
				for (int num4 = 0; num4 < num2 / 3; num4++)
				{
					list.Add(num3);
					list.Add(num3 + 2);
					list.Add(num3 + 1);
					num3 += 3;
				}
			}
			else
			{
				int num5 = 0;
				foreach (int item in VCount)
				{
					switch (item)
					{
					case 3:
						list.Add(num5);
						list.Add(num5 + 2);
						list.Add(num5 + 1);
						num5 += 3;
						break;
					case 4:
						list.Add(num5);
						list.Add(num5 + 2);
						list.Add(num5 + 1);
						list.Add(num5 + 2);
						list.Add(num5);
						list.Add(num5 + 3);
						num5 += 4;
						break;
					default:
						throw new NotSupportedException("Collada polygons with less than 3 or more than 4 vertices are not supported.");
					}
				}
			}
			int vertexStride = modelBuffersData.VertexDeclaration.VertexStride;
			int num6 = modelBuffersData.Vertices.Length;
			modelBuffersData.Vertices = ExtendArray(modelBuffersData.Vertices, list.Count * vertexStride);
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream(modelBuffersData.Vertices, num6, list.Count * vertexStride)))
			{
				bool flag = false;
				foreach (KeyValuePair<VertexElement, ColladaInput> item2 in dictionary)
				{
					VertexElement key = item2.Key;
					ColladaInput value = item2.Value;
					if (key.Semantic.StartsWith("POSITION"))
					{
						for (int num7 = 0; num7 < list.Count; num7++)
						{
							float[] array = value.Source.Accessor.Source.Array;
							int offset = value.Source.Accessor.Offset;
							int stride = value.Source.Accessor.Stride;
							int num8 = P[list[num7] * Inputs.Count + value.Offset];
							binaryWriter.BaseStream.Position = num7 * vertexStride + key.Offset;
							float num9 = array[offset + stride * num8];
							float num10 = array[offset + stride * num8 + 1];
							float num11 = array[offset + stride * num8 + 2];
							modelMeshPartData.BoundingBox = (flag ? BoundingBox.Union(modelMeshPartData.BoundingBox, new Vector3(num9, num10, num11)) : new BoundingBox(num9, num10, num11, num9, num10, num11));
							flag = true;
							binaryWriter.Write(num9);
							binaryWriter.Write(num10);
							binaryWriter.Write(num11);
						}
					}
					else if (key.Semantic.StartsWith("NORMAL"))
					{
						for (int num12 = 0; num12 < list.Count; num12++)
						{
							float[] array2 = value.Source.Accessor.Source.Array;
							int offset2 = value.Source.Accessor.Offset;
							int stride2 = value.Source.Accessor.Stride;
							int num13 = P[list[num12] * Inputs.Count + value.Offset];
							binaryWriter.BaseStream.Position = num12 * vertexStride + key.Offset;
							float num14 = array2[offset2 + stride2 * num13];
							float num15 = array2[offset2 + stride2 * num13 + 1];
							float num16 = array2[offset2 + stride2 * num13 + 2];
							float num17 = 1f / MathUtils.Sqrt(num14 * num14 + num15 * num15 + num16 * num16);
							binaryWriter.Write(num17 * num14);
							binaryWriter.Write(num17 * num15);
							binaryWriter.Write(num17 * num16);
						}
					}
					else if (key.Semantic.StartsWith("TEXCOORD"))
					{
						for (int num18 = 0; num18 < list.Count; num18++)
						{
							float[] array3 = value.Source.Accessor.Source.Array;
							int offset3 = value.Source.Accessor.Offset;
							int stride3 = value.Source.Accessor.Stride;
							int num19 = P[list[num18] * Inputs.Count + value.Offset];
							binaryWriter.BaseStream.Position = num18 * vertexStride + key.Offset;
							binaryWriter.Write(array3[offset3 + stride3 * num19]);
							binaryWriter.Write(1f - array3[offset3 + stride3 * num19 + 1]);
						}
					}
					else
					{
						if (!key.Semantic.StartsWith("COLOR"))
						{
							throw new Exception();
						}
						for (int num20 = 0; num20 < list.Count; num20++)
						{
							float[] array4 = value.Source.Accessor.Source.Array;
							int offset4 = value.Source.Accessor.Offset;
							int stride4 = value.Source.Accessor.Stride;
							int num21 = P[list[num20] * Inputs.Count + value.Offset];
							binaryWriter.BaseStream.Position = num20 * vertexStride + key.Offset;
							binaryWriter.Write(new Color(array4[offset4 + stride4 * num21], array4[offset4 + stride4 * num21 + 1], array4[offset4 + stride4 * num21 + 2], array4[offset4 + stride4 * num21 + 3]).PackedValue);
						}
					}
				}
			}
			modelMeshPartData.StartIndex = num6 / vertexStride;
			modelMeshPartData.IndicesCount = list.Count;
			modelMeshData.MeshParts.Add(modelMeshPartData);
			modelMeshData.BoundingBox = ((modelMeshData.MeshParts.Count > 1) ? BoundingBox.Union(modelMeshData.BoundingBox, modelMeshPartData.BoundingBox) : modelMeshPartData.BoundingBox);
		}

		public void Save(XElement node)
		{
			node.SetAttributeValue("count", P.Count / Inputs.Count / 3);
			foreach (ColladaInput input in Inputs)
			{
				input.Save(CreateElement(node, ColladaRoot.Namespace + "input"));
			}
			CreateElement(node, ColladaRoot.Namespace + "p").Value = string.Join(" ", P.Select((int n) => n.ToString(CultureInfo.InvariantCulture)).ToArray());
		}
	}

	private class ColladaInput
	{
		public int Offset;

		public string Semantic;

		public int Set;

		public ColladaSource Source;

		public ColladaVertices Vertices;

		public ColladaInput()
		{
		}

		public ColladaInput(ColladaRoot colladaRoot, XElement node)
		{
			Offset = int.Parse(node.Attribute("offset").Value, CultureInfo.InvariantCulture);
			Semantic = node.Attribute("semantic").Value;
			XAttribute xAttribute = node.Attribute("set");
			if (xAttribute != null)
			{
				Set = int.Parse(xAttribute.Value, CultureInfo.InvariantCulture);
			}
			ColladaNameId colladaNameId = colladaRoot.ObjectsById[node.Attribute("source").Value.Substring(1)];
			if (colladaNameId is ColladaVertices)
			{
				Vertices = (ColladaVertices)colladaNameId;
				Source = Vertices.Source;
				Semantic = Vertices.Semantic;
			}
			else
			{
				Source = (ColladaSource)colladaNameId;
			}
		}

		public void Save(XElement node)
		{
			node.SetAttributeValue("semantic", Semantic);
			if (Set != 0)
			{
				node.SetAttributeValue("set", Set.ToString(CultureInfo.InvariantCulture));
			}
			if (Vertices != null)
			{
				node.SetAttributeValue("source", "#" + Vertices.Id);
			}
			else
			{
				node.SetAttributeValue("source", "#" + Source.Id);
			}
			node.SetAttributeValue("offset", Offset.ToString(CultureInfo.InvariantCulture));
		}
	}

	public static bool IsColladaStream(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		bool result = false;
		long position = stream.Position;
		try
		{
			XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings
			{
				IgnoreComments = true,
				IgnoreWhitespace = true
			});
			while (xmlReader.Read())
			{
				if (xmlReader.NodeType == XmlNodeType.Element)
				{
					if (xmlReader.LocalName == "COLLADA")
					{
						result = true;
					}
					break;
				}
			}
		}
		catch (XmlException)
		{
		}
		stream.Position = position;
		return result;
	}

	public static ModelData Load(Stream stream)
	{
		ColladaRoot colladaRoot = new ColladaRoot(XElement.Load(stream));
		ModelData modelData = new ModelData();
		colladaRoot.Save(modelData);
		return modelData;
	}

	public static void Save(ModelData modelData, Stream stream)
	{
		ColladaRoot colladaRoot = new ColladaRoot(modelData);
		XElement xElement = CreateElement(null, ColladaRoot.Namespace + "COLLADA");
		colladaRoot.Save(xElement);
		XmlWriterSettings settings = new XmlWriterSettings
		{
			Indent = true,
			Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
		};
		using XmlWriter writer = XmlWriter.Create(stream, settings);
		xElement.Save(writer);
	}

	private static T[] ExtendArray<T>(T[] array, int extensionLength)
	{
		T[] array2 = new T[array.Length + extensionLength];
		Array.Copy(array, array2, array.Length);
		return array2;
	}

	private static void IndexVertices(int vertexStride, byte[] vertices, out byte[] resultVertices, out byte[] resultIndices)
	{
		int num = vertices.Length / vertexStride;
		Dictionary<Vertex, ushort> dictionary = new Dictionary<Vertex, ushort>();
		resultIndices = new byte[2 * num];
		for (int i = 0; i < num; i++)
		{
			Vertex key = new Vertex(vertices, i * vertexStride, vertexStride);
			if (!dictionary.TryGetValue(key, out var value))
			{
				value = (ushort)dictionary.Count;
				dictionary.Add(key, value);
			}
			resultIndices[i * 2] = (byte)value;
			resultIndices[i * 2 + 1] = (byte)(value >> 8);
		}
		resultVertices = new byte[dictionary.Count * vertexStride];
		foreach (KeyValuePair<Vertex, ushort> item in dictionary)
		{
			Vertex key2 = item.Key;
			ushort value2 = item.Value;
			Array.Copy(key2.Data, key2.Start, resultVertices, value2 * vertexStride, key2.Count);
		}
	}

	private static XElement CreateElement(XElement parent, XName name)
	{
		XElement xElement = new XElement(name);
		parent?.Add(xElement);
		return xElement;
	}
}
