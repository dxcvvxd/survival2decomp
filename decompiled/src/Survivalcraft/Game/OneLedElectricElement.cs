using Engine;

namespace Game;

public class OneLedElectricElement : MountedElectricElement
{
	private SubsystemGlow m_subsystemGlow;

	private float m_voltage;

	private Color m_color;

	private GlowPoint m_glowPoint;

	public OneLedElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
		m_subsystemGlow = subsystemElectricity.Project.FindSubsystem<SubsystemGlow>(throwOnError: true);
	}

	public override void OnAdded()
	{
		CellFace cellFace = base.CellFaces[0];
		int data = Terrain.ExtractData(base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z));
		int mountingFace = FourLedBlock.GetMountingFace(data);
		m_color = LedBlock.LedColors[FourLedBlock.GetColor(data)];
		Vector3 vector = new Vector3((float)cellFace.X + 0.5f, (float)cellFace.Y + 0.5f, (float)cellFace.Z + 0.5f);
		Vector3 vector2 = CellFace.FaceToVector3(mountingFace);
		Vector3 vector3 = ((mountingFace < 4) ? Vector3.UnitY : Vector3.UnitX);
		Vector3 right = Vector3.Cross(vector2, vector3);
		m_glowPoint = m_subsystemGlow.AddGlowPoint();
		m_glowPoint.Position = vector - 0.4375f * CellFace.FaceToVector3(mountingFace);
		m_glowPoint.Forward = vector2;
		m_glowPoint.Up = vector3;
		m_glowPoint.Right = right;
		m_glowPoint.Color = Color.Transparent;
		m_glowPoint.Size = 0.52f;
		m_glowPoint.FarSize = 0.52f;
		m_glowPoint.FarDistance = 1f;
		m_glowPoint.Type = GlowPointType.Square;
	}

	public override void OnRemoved()
	{
		m_subsystemGlow.RemoveGlowPoint(m_glowPoint);
	}

	public override bool Simulate()
	{
		float voltage = m_voltage;
		m_voltage = 0f;
		foreach (ElectricConnection connection in base.Connections)
		{
			if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != ElectricConnectorType.Input)
			{
				m_voltage = MathUtils.Max(m_voltage, connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
			}
		}
		if (m_voltage != voltage)
		{
			int num = (int)MathUtils.Round(m_voltage * 15f);
			if (num >= 8)
			{
				m_glowPoint.Color = LedBlock.LedColors[MathUtils.Clamp(num - 8, 0, 7)];
			}
			else
			{
				m_glowPoint.Color = Color.Transparent;
			}
		}
		return false;
	}
}
