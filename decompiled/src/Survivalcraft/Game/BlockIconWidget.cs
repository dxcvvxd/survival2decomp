using Engine;
using Engine.Graphics;

namespace Game;

public class BlockIconWidget : Widget
{
	private Matrix m_viewMatrix;

	private int m_value;

	public DrawBlockEnvironmentData DrawBlockEnvironmentData { get; private set; }

	public Vector2 Size { get; set; }

	public float Depth { get; set; }

	public Color Color { get; set; }

	public Matrix? CustomViewMatrix { get; set; }

	public int Value
	{
		get
		{
			return m_value;
		}
		set
		{
			if (m_value == 0 || value != m_value)
			{
				m_value = value;
				Block block = BlocksManager.Blocks[Contents];
				m_viewMatrix = Matrix.CreateLookAt(block.GetIconViewOffset(Value, DrawBlockEnvironmentData), new Vector3(0f, 0f, 0f), Vector3.UnitY);
			}
		}
	}

	public int Contents
	{
		get
		{
			return Terrain.ExtractContents(Value);
		}
		set
		{
			Value = Terrain.ReplaceContents(Value, value);
		}
	}

	public int Light
	{
		get
		{
			return Terrain.ExtractLight(Value);
		}
		set
		{
			Value = Terrain.ReplaceLight(Value, value);
		}
	}

	public int Data
	{
		get
		{
			return Terrain.ExtractData(Value);
		}
		set
		{
			Value = Terrain.ReplaceData(Value, value);
		}
	}

	public float Scale { get; set; }

	public BlockIconWidget()
	{
		Size = new Vector2(float.PositiveInfinity);
		IsHitTestVisible = false;
		Light = 15;
		Depth = 1f;
		Color = Color.White;
		DrawBlockEnvironmentData = new DrawBlockEnvironmentData();
		Scale = 1f;
	}

	public override void Draw(DrawContext dc)
	{
		Block obj = BlocksManager.Blocks[Contents];
		Viewport viewport = Display.Viewport;
		float num = MathUtils.Min(base.ActualSize.X, base.ActualSize.Y) * Scale;
		Matrix matrix = Matrix.CreateOrthographic(3.6f, 3.6f, -10f - 1f * Depth, 10f - 1f * Depth);
		Matrix matrix2 = MatrixUtils.CreateScaleTranslation(num, 0f - num, base.ActualSize.X / 2f, base.ActualSize.Y / 2f) * base.GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)viewport.Width, -2f / (float)viewport.Height, -1f, 1f);
		DrawBlockEnvironmentData.DrawBlockMode = DrawBlockMode.UI;
		DrawBlockEnvironmentData.ViewProjectionMatrix = (CustomViewMatrix.HasValue ? CustomViewMatrix.Value : m_viewMatrix) * matrix * matrix2;
		float iconViewScale = BlocksManager.Blocks[Contents].GetIconViewScale(Value, DrawBlockEnvironmentData);
		Matrix matrix3 = (CustomViewMatrix.HasValue ? Matrix.Identity : Matrix.CreateTranslation(BlocksManager.Blocks[Contents].GetIconBlockOffset(Value, DrawBlockEnvironmentData)));
		obj.DrawBlock(dc.PrimitivesRenderer3D, Value, base.GlobalColorTransform, iconViewScale, ref matrix3, DrawBlockEnvironmentData);
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		base.IsDrawRequired = true;
		base.DesiredSize = Size;
	}
}
