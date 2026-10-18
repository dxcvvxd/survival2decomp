using Engine;

namespace Game;

public class TppCamera : BasePerspectiveCamera
{
	private Vector3 m_position;

	public override bool UsesMovementControls => false;

	public override bool IsEntityControlEnabled => true;

	public TppCamera(GameWidget gameWidget)
		: base(gameWidget)
	{
	}

	public override void Activate(Camera previousCamera)
	{
		m_position = previousCamera.ViewPosition;
		SetupPerspectiveCamera(m_position, previousCamera.ViewDirection, previousCamera.ViewUp);
	}

	public override void Update(float dt)
	{
		if (base.GameWidget.Target == null)
		{
			return;
		}
		Matrix matrix = Matrix.CreateFromQuaternion(base.GameWidget.Target.ComponentCreatureModel.EyeRotation);
		matrix.Translation = base.GameWidget.Target.ComponentBody.Position + 0.9f * base.GameWidget.Target.ComponentBody.BoxSize.Y * Vector3.UnitY;
		Vector3 vector = -2.25f * matrix.Forward + 1.75f * matrix.Up;
		Vector3 vector2 = matrix.Translation + vector;
		if (Vector3.Distance(vector2, m_position) < 10f)
		{
			Vector3 vector3 = vector2 - m_position;
			float num = 3f * dt;
			m_position += num * vector3;
		}
		else
		{
			m_position = vector2;
		}
		Vector3 vector4 = m_position - matrix.Translation;
		float? num2 = null;
		Vector3 vector5 = Vector3.Normalize(Vector3.Cross(vector4, Vector3.UnitY));
		Vector3 vector6 = Vector3.Normalize(Vector3.Cross(vector4, vector5));
		SubsystemTerrain subsystemTerrain = base.GameWidget.SubsystemGameWidgets.SubsystemTerrain;
		for (int i = 0; i <= 0; i++)
		{
			for (int j = 0; j <= 0; j++)
			{
				Vector3 vector7 = 0.6f * (vector5 * i + vector6 * j);
				Vector3 vector8 = matrix.Translation + vector7;
				Vector3 end = vector8 + vector4 + Vector3.Normalize(vector4) * 0.6f;
				TerrainRaycastResult? terrainRaycastResult = subsystemTerrain.Raycast(vector8, end, useInteractionBoxes: false, skipAirBlocks: true, delegate(int value, float distance)
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
					for (int k = 0; k < 6; k++)
					{
						if (!block.IsFaceTransparent(subsystemTerrain, k, value))
						{
							return true;
						}
					}
					return false;
				});
				if (terrainRaycastResult.HasValue)
				{
					num2 = (num2.HasValue ? MathUtils.Min(num2.Value, terrainRaycastResult.Value.Distance) : terrainRaycastResult.Value.Distance);
				}
			}
		}
		Vector3 vector9 = ((!num2.HasValue) ? (matrix.Translation + vector4) : (matrix.Translation + Vector3.Normalize(vector4) * MathUtils.Max(num2.Value - 0.6f, 0.2f)));
		SetupPerspectiveCamera(vector9, matrix.Translation - vector9, Vector3.UnitY);
	}
}
