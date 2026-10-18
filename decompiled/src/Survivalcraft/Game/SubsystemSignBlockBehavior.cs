using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;
using TemplatesDatabase;

namespace Game;

public class SubsystemSignBlockBehavior : SubsystemBlockBehavior, IDrawable, IUpdateable
{
	private class TextData
	{
		public Point3 Point;

		public string[] Lines = new string[4]
		{
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty
		};

		public Color[] Colors = new Color[4]
		{
			Color.Black,
			Color.Black,
			Color.Black,
			Color.Black
		};

		public string Url = string.Empty;

		public int? TextureLocation;

		public float UsedTextureWidth;

		public float UsedTextureHeight;

		public float Distance;

		public int ToBeRenderedFrame;

		public int Light;
	}

	private const float m_maxVisibilityDistanceSqr = 400f;

	private const float m_minUpdateDistance = 2f;

	private const int m_textWidth = 128;

	private const int m_textHeight = 32;

	private const int m_maxTexts = 32;

	private SubsystemGameWidgets m_subsystemViews;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemGameInfo m_subsystemGameInfo;

	private Dictionary<Point3, TextData> m_textsByPoint = new Dictionary<Point3, TextData>();

	private TextData[] m_textureLocations = new TextData[32];

	private List<TextData> m_nearTexts = new List<TextData>();

	private BitmapFont m_font = ContentManager.Get<BitmapFont>("Fonts/SignFont");

	private RenderTarget2D m_renderTarget;

	private List<Vector3> m_lastUpdatePositions = new List<Vector3>();

	private PrimitivesRenderer2D m_primitivesRenderer2D = new PrimitivesRenderer2D();

	private PrimitivesRenderer3D m_primitivesRenderer3D = new PrimitivesRenderer3D();

	public bool ShowSignsTexture;

	public bool CopySignsText;

	private static int[] m_drawOrders = new int[1] { 50 };

	public override int[] HandledBlocks => new int[4] { 97, 98, 210, 211 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => m_drawOrders;

	public SignData GetSignData(Point3 point)
	{
		if (m_textsByPoint.TryGetValue(point, out var value))
		{
			return new SignData
			{
				Lines = value.Lines.ToArray(),
				Colors = value.Colors.ToArray(),
				Url = value.Url
			};
		}
		return null;
	}

	public void SetSignData(Point3 point, string[] lines, Color[] colors, string url)
	{
		TextData textData = new TextData();
		textData.Point = point;
		for (int i = 0; i < 4; i++)
		{
			textData.Lines[i] = lines[i];
			textData.Colors[i] = colors[i];
		}
		textData.Url = url;
		m_textsByPoint[point] = textData;
		m_lastUpdatePositions.Clear();
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellValueFast = base.SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
		int num = Terrain.ExtractContents(cellValueFast);
		int data = Terrain.ExtractData(cellValueFast);
		Block block = BlocksManager.Blocks[num];
		if (block is AttachedSignBlock)
		{
			Point3 point = CellFace.FaceToPoint3(AttachedSignBlock.GetFace(data));
			int x2 = x - point.X;
			int y2 = y - point.Y;
			int z2 = z - point.Z;
			int cellContents = base.SubsystemTerrain.Terrain.GetCellContents(x2, y2, z2);
			if (!BlocksManager.Blocks[cellContents].IsCollidable)
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}
		else if (block is PostedSignBlock)
		{
			int num2 = (PostedSignBlock.GetHanging(data) ? base.SubsystemTerrain.Terrain.GetCellContents(x, y + 1, z) : base.SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z));
			if (!BlocksManager.Blocks[num2].IsCollidable)
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
		Point3 point = new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure)
		{
			SignData signData = GetSignData(point);
			if (signData != null && !string.IsNullOrEmpty(signData.Url))
			{
				WebBrowserManager.LaunchBrowser(signData.Url);
			}
		}
		else if (componentMiner.ComponentPlayer != null)
		{
			DialogsManager.ShowDialog(componentMiner.ComponentPlayer.GuiWidget, new EditSignDialog(this, point));
		}
		return true;
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		Point3 key = new Point3(x, y, z);
		m_textsByPoint.Remove(key);
		m_lastUpdatePositions.Clear();
	}

	public void Update(float dt)
	{
		UpdateRenderTarget();
	}

	public void Draw(Camera camera, int drawOrder)
	{
		DrawSigns(camera);
	}

	protected override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemViews = base.Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		foreach (ValuesDictionary value11 in valuesDictionary.GetValue<ValuesDictionary>("Texts").Values)
		{
			Point3 value = value11.GetValue<Point3>("Point");
			string value2 = value11.GetValue("Line1", string.Empty);
			string value3 = value11.GetValue("Line2", string.Empty);
			string value4 = value11.GetValue("Line3", string.Empty);
			string value5 = value11.GetValue("Line4", string.Empty);
			Color value6 = value11.GetValue("Color1", Color.Black);
			Color value7 = value11.GetValue("Color2", Color.Black);
			Color value8 = value11.GetValue("Color3", Color.Black);
			Color value9 = value11.GetValue("Color4", Color.Black);
			string value10 = value11.GetValue("Url", string.Empty);
			SetSignData(value, new string[4] { value2, value3, value4, value5 }, new Color[4] { value6, value7, value8, value9 }, value10);
		}
		CreateRenderTarget();
		Display.DeviceReset += Display_DeviceReset;
	}

	protected override void Save(ValuesDictionary valuesDictionary)
	{
		base.Save(valuesDictionary);
		int num = 0;
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Texts", valuesDictionary2);
		foreach (TextData value in m_textsByPoint.Values)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary3.SetValue("Point", value.Point);
			if (!string.IsNullOrEmpty(value.Lines[0]))
			{
				valuesDictionary3.SetValue("Line1", value.Lines[0]);
			}
			if (!string.IsNullOrEmpty(value.Lines[1]))
			{
				valuesDictionary3.SetValue("Line2", value.Lines[1]);
			}
			if (!string.IsNullOrEmpty(value.Lines[2]))
			{
				valuesDictionary3.SetValue("Line3", value.Lines[2]);
			}
			if (!string.IsNullOrEmpty(value.Lines[3]))
			{
				valuesDictionary3.SetValue("Line4", value.Lines[3]);
			}
			if (value.Colors[0] != Color.Black)
			{
				valuesDictionary3.SetValue("Color1", value.Colors[0]);
			}
			if (value.Colors[1] != Color.Black)
			{
				valuesDictionary3.SetValue("Color2", value.Colors[1]);
			}
			if (value.Colors[2] != Color.Black)
			{
				valuesDictionary3.SetValue("Color3", value.Colors[2]);
			}
			if (value.Colors[3] != Color.Black)
			{
				valuesDictionary3.SetValue("Color4", value.Colors[3]);
			}
			if (!string.IsNullOrEmpty(value.Url))
			{
				valuesDictionary3.SetValue("Url", value.Url);
			}
			valuesDictionary2.SetValue(num++.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		Utilities.Dispose(ref m_renderTarget);
		Display.DeviceReset -= Display_DeviceReset;
	}

	private void Display_DeviceReset()
	{
		InvalidateRenderTarget();
	}

	private void CreateRenderTarget()
	{
		m_renderTarget = new RenderTarget2D(128, 1024, 1, ColorFormat.Rgba8888, DepthFormat.None);
	}

	private void InvalidateRenderTarget()
	{
		m_lastUpdatePositions.Clear();
		for (int i = 0; i < m_textureLocations.Length; i++)
		{
			m_textureLocations[i] = null;
		}
		foreach (TextData value in m_textsByPoint.Values)
		{
			value.TextureLocation = null;
		}
	}

	private void RenderText(FontBatch2D fontBatch, FlatBatch2D flatBatch, TextData textData)
	{
		if (!textData.TextureLocation.HasValue)
		{
			return;
		}
		List<string> list = new List<string>();
		List<Color> list2 = new List<Color>();
		for (int i = 0; i < textData.Lines.Length; i++)
		{
			if (!string.IsNullOrEmpty(textData.Lines[i]))
			{
				list.Add(textData.Lines[i].Replace("\\", "").ToUpper());
				list2.Add(textData.Colors[i]);
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		float num = list.Max((string l) => l.Length) * 8;
		float num2 = list.Count * 8;
		float num3 = 4f;
		float num4;
		float num5;
		if (num / num2 < num3)
		{
			num4 = num2 * num3;
			num5 = num2;
		}
		else
		{
			num4 = num;
			num5 = num / num3;
		}
		bool flag = !string.IsNullOrEmpty(textData.Url);
		for (int num6 = 0; num6 < list.Count; num6++)
		{
			Vector2 position = new Vector2(num4 / 2f, (float)(num6 * 8 + textData.TextureLocation.Value * 32) + (num5 - num2) / 2f);
			fontBatch.QueueText(list[num6], position, 0f, flag ? new Color(0, 0, 64) : list2[num6], TextAnchor.HorizontalCenter);
			if (flag)
			{
				flatBatch.QueueLine(new Vector2(position.X - (float)(list[num6].Length * 8 / 2), position.Y + 8f), new Vector2(position.X + (float)(list[num6].Length * 8 / 2), position.Y + 8f), 0f, new Color(0, 0, 64));
			}
		}
		textData.UsedTextureWidth = num4;
		textData.UsedTextureHeight = num5;
	}

	private void UpdateRenderTarget()
	{
		bool flag = false;
		foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets)
		{
			bool flag2 = false;
			foreach (Vector3 lastUpdatePosition in m_lastUpdatePositions)
			{
				if (Vector3.DistanceSquared(gameWidget.ActiveCamera.ViewPosition, lastUpdatePosition) < 4f)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		m_lastUpdatePositions.Clear();
		m_lastUpdatePositions.AddRange(m_subsystemViews.GameWidgets.Select((GameWidget v) => v.ActiveCamera.ViewPosition));
		m_nearTexts.Clear();
		foreach (TextData value in m_textsByPoint.Values)
		{
			Point3 point = value.Point;
			float num = m_subsystemViews.CalculateSquaredDistanceFromNearestView(new Vector3(point));
			if (num <= 400f)
			{
				value.Distance = num;
				m_nearTexts.Add(value);
			}
		}
		m_nearTexts.Sort((TextData d1, TextData d2) => Comparer<float>.Default.Compare(d1.Distance, d2.Distance));
		if (m_nearTexts.Count > 32)
		{
			m_nearTexts.RemoveRange(32, m_nearTexts.Count - 32);
		}
		foreach (TextData nearText in m_nearTexts)
		{
			nearText.ToBeRenderedFrame = Time.FrameIndex;
		}
		bool flag3 = false;
		for (int num2 = 0; num2 < MathUtils.Min(m_nearTexts.Count, 32); num2++)
		{
			TextData textData = m_nearTexts[num2];
			if (textData.TextureLocation.HasValue)
			{
				continue;
			}
			int num3 = m_textureLocations.FirstIndex((TextData d) => d == null);
			if (num3 < 0)
			{
				num3 = m_textureLocations.FirstIndex((TextData d) => d.ToBeRenderedFrame != Time.FrameIndex);
			}
			if (num3 >= 0)
			{
				TextData textData2 = m_textureLocations[num3];
				if (textData2 != null)
				{
					textData2.TextureLocation = null;
					m_textureLocations[num3] = null;
				}
				m_textureLocations[num3] = textData;
				textData.TextureLocation = num3;
				flag3 = true;
			}
		}
		if (!flag3)
		{
			return;
		}
		RenderTarget2D renderTarget = Display.RenderTarget;
		Display.RenderTarget = m_renderTarget;
		try
		{
			Display.Clear(new Vector4(Color.Transparent));
			FlatBatch2D flatBatch = m_primitivesRenderer2D.FlatBatch(0, DepthStencilState.None, null, BlendState.Opaque);
			FontBatch2D fontBatch = m_primitivesRenderer2D.FontBatch(m_font, 1, DepthStencilState.None, null, BlendState.Opaque, SamplerState.PointClamp);
			for (int num4 = 0; num4 < m_textureLocations.Length; num4++)
			{
				TextData textData3 = m_textureLocations[num4];
				if (textData3 != null)
				{
					RenderText(fontBatch, flatBatch, textData3);
				}
			}
			m_primitivesRenderer2D.Flush();
		}
		finally
		{
			Display.RenderTarget = renderTarget;
		}
	}

	private void DrawSigns(Camera camera)
	{
		if (m_nearTexts.Count <= 0)
		{
			return;
		}
		TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(m_renderTarget, useAlphaTest: false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, null, SamplerState.PointClamp);
		foreach (TextData nearText in m_nearTexts)
		{
			if (!nearText.TextureLocation.HasValue)
			{
				continue;
			}
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(nearText.Point.X, nearText.Point.Y, nearText.Point.Z);
			int num = Terrain.ExtractContents(cellValue);
			if (!(BlocksManager.Blocks[num] is SignBlock signBlock))
			{
				continue;
			}
			int data = Terrain.ExtractData(cellValue);
			BlockMesh signSurfaceBlockMesh = signBlock.GetSignSurfaceBlockMesh(data);
			if (signSurfaceBlockMesh != null)
			{
				TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(nearText.Point.X, nearText.Point.Z);
				if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidVertices1)
				{
					nearText.Light = Terrain.ExtractLight(cellValue);
				}
				float num2 = LightingManager.LightIntensityByLightValue[nearText.Light];
				Color color = new Color(num2, num2, num2);
				float x = 0f;
				float x2 = nearText.UsedTextureWidth / 128f;
				float x3 = (float)nearText.TextureLocation.Value / 32f;
				float x4 = ((float)nearText.TextureLocation.Value + nearText.UsedTextureHeight / 32f) / 32f;
				Vector3 signSurfaceNormal = signBlock.GetSignSurfaceNormal(data);
				Vector3 vector = new Vector3(nearText.Point.X, nearText.Point.Y, nearText.Point.Z);
				float num3 = Vector3.Dot(camera.ViewPosition - (vector + new Vector3(0.5f)), signSurfaceNormal);
				Vector3 vector2 = MathUtils.Max(0.01f * num3, 0.005f) * signSurfaceNormal;
				for (int i = 0; i < signSurfaceBlockMesh.Indices.Count / 3; i++)
				{
					BlockMeshVertex blockMeshVertex = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3]];
					BlockMeshVertex blockMeshVertex2 = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3 + 1]];
					BlockMeshVertex blockMeshVertex3 = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3 + 2]];
					Vector3 p = blockMeshVertex.Position + vector + vector2;
					Vector3 p2 = blockMeshVertex2.Position + vector + vector2;
					Vector3 p3 = blockMeshVertex3.Position + vector + vector2;
					Vector2 textureCoordinates = blockMeshVertex.TextureCoordinates;
					Vector2 textureCoordinates2 = blockMeshVertex2.TextureCoordinates;
					Vector2 textureCoordinates3 = blockMeshVertex3.TextureCoordinates;
					textureCoordinates.X = MathUtils.Lerp(x, x2, textureCoordinates.X);
					textureCoordinates2.X = MathUtils.Lerp(x, x2, textureCoordinates2.X);
					textureCoordinates3.X = MathUtils.Lerp(x, x2, textureCoordinates3.X);
					textureCoordinates.Y = MathUtils.Lerp(x3, x4, textureCoordinates.Y);
					textureCoordinates2.Y = MathUtils.Lerp(x3, x4, textureCoordinates2.Y);
					textureCoordinates3.Y = MathUtils.Lerp(x3, x4, textureCoordinates3.Y);
					texturedBatch3D.QueueTriangle(p, p2, p3, textureCoordinates, textureCoordinates2, textureCoordinates3, color);
				}
			}
		}
		m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
	}
}
