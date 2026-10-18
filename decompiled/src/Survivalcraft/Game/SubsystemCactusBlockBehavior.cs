using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemCactusBlockBehavior : SubsystemPollableBlockBehavior
{
	private SubsystemTime m_subsystemTime;

	private SubsystemCellChangeQueue m_subsystemCellChangeQueue;

	private SubsystemGameInfo m_subsystemGameInfo;

	private Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 127 };

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellContents = base.SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
		if (cellContents != 7 && cellContents != 127)
		{
			base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		if (Terrain.ExtractContents(cellValue) == 0 && Terrain.ExtractLight(cellValue) >= 12)
		{
			int cellContents = base.SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
			int cellContents2 = base.SubsystemTerrain.Terrain.GetCellContents(x, y - 2, z);
			if ((cellContents != 127 || cellContents2 != 127) && m_random.Float(0f, 1f) < 0.25f)
			{
				m_subsystemCellChangeQueue.QueueCellChange(x, y + 1, z, Terrain.MakeBlockValue(127, 0, 0));
			}
		}
	}

	public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody)
	{
		componentBody.Entity.FindComponent<ComponentCreature>()?.ComponentHealth.Injure(0.01f * MathUtils.Abs(velocity), null, ignoreInvulnerability: false, "Spiked by cactus");
	}

	protected override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemCellChangeQueue = base.Project.FindSubsystem<SubsystemCellChangeQueue>(throwOnError: true);
		m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		base.Load(valuesDictionary);
	}
}
