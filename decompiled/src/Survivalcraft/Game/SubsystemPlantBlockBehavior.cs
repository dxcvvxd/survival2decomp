using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemPlantBlockBehavior : SubsystemPollableBlockBehavior
{
	private SubsystemTime m_subsystemTime;

	private SubsystemCellChangeQueue m_subsystemCellChangeQueue;

	private SubsystemGameInfo m_subsystemGameInfo;

	private SubsystemSeasons m_subsystemSeasons;

	private Random m_random = new Random();

	public override int[] HandledBlocks => new int[11]
	{
		19, 20, 24, 25, 28, 99, 131, 244, 132, 174,
		204
	};

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int num = Terrain.ExtractContents(base.SubsystemTerrain.Terrain.GetCellValue(x, y, z));
		int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
		int num2 = Terrain.ExtractContents(cellValue);
		switch (num)
		{
		case 131:
		case 244:
			if (num2 != 8 && num2 != 2 && num2 != 168)
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		case 132:
		{
			Block block = BlocksManager.Blocks[num2];
			if (block.IsFaceTransparent(base.SubsystemTerrain, 4, cellValue) && !(block is FenceBlock))
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		}
		default:
			if (num2 != 8 && num2 != 2 && num2 != 7 && num2 != 168)
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		}
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		Lifecycle(value, x, y, z, applyImmediately: true);
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living)
		{
			Grow(value, x, y, z, pollPass);
			Lifecycle(value, x, y, z, applyImmediately: false);
		}
	}

	protected override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemCellChangeQueue = base.Project.FindSubsystem<SubsystemCellChangeQueue>(throwOnError: true);
		m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSeasons = base.Project.FindSubsystem<SubsystemSeasons>(throwOnError: true);
	}

	private void Grow(int value, int x, int y, int z, int pollPass)
	{
		if (y <= 0 || y >= 255)
		{
			return;
		}
		int num = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[num];
		if (num == 19)
		{
			GrowTallGrass(value, x, y, z, pollPass);
			return;
		}
		if (block is FlowerBlock)
		{
			GrowFlower(value, x, y, z, pollPass);
			return;
		}
		switch (num)
		{
		case 174:
			GrowRye(value, x, y, z, pollPass);
			break;
		case 204:
			GrowCotton(value, x, y, z, pollPass);
			break;
		case 131:
			GrowPumpkin(value, x, y, z, pollPass);
			break;
		}
	}

	private void GrowTallGrass(int value, int x, int y, int z, int pollPass)
	{
		int data = Terrain.ExtractData(value);
		if (TallGrassBlock.GetIsSmall(data) && Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) >= 9)
		{
			int data2 = TallGrassBlock.SetIsSmall(data, isSmall: false);
			int value2 = Terrain.ReplaceData(value, data2);
			m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value2);
		}
	}

	private void GrowFlower(int value, int x, int y, int z, int pollPass)
	{
		int data = Terrain.ExtractData(value);
		if (FlowerBlock.GetIsSmall(data) && Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) >= 9)
		{
			int data2 = FlowerBlock.SetIsSmall(data, isSmall: false);
			int value2 = Terrain.ReplaceData(value, data2);
			m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value2);
		}
	}

	private void GrowRye(int value, int x, int y, int z, int pollPass)
	{
		if (Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) < 9)
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		int size = RyeBlock.GetSize(data);
		if (size == 7)
		{
			return;
		}
		if (RyeBlock.GetIsWild(data))
		{
			if (size < 7)
			{
				int data2 = RyeBlock.SetSize(RyeBlock.SetIsWild(data, isWild: true), size + 1);
				int value2 = Terrain.ReplaceData(value, data2);
				m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value2);
			}
			return;
		}
		int cellValueFast = base.SubsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
		if (Terrain.ExtractContents(cellValueFast) == 168)
		{
			int data3 = Terrain.ExtractData(cellValueFast);
			bool hydration = SoilBlock.GetHydration(data3);
			int nitrogen = SoilBlock.GetNitrogen(data3);
			int num = base.SubsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
			int num2 = 4;
			float num3 = 0.8f;
			if (nitrogen > 0)
			{
				num2--;
				num3 -= 0.4f;
			}
			if (hydration)
			{
				num2--;
				num3 -= 0.4f;
			}
			if (num <= 4)
			{
				num2 += 4;
			}
			if (pollPass % MathUtils.Max(num2, 1) == 0 || !(num3 < 1f))
			{
				int data4 = RyeBlock.SetSize(data, MathUtils.Min(size + 1, 7));
				if (m_random.Float(0f, 1f) < num3 && size == 3)
				{
					data4 = RyeBlock.SetIsWild(data4, isWild: true);
				}
				int value3 = Terrain.ReplaceData(value, data4);
				m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value3);
				if (size + 1 == 7)
				{
					int data5 = SoilBlock.SetNitrogen(data3, MathUtils.Max(nitrogen - 1, 0));
					int value4 = Terrain.ReplaceData(cellValueFast, data5);
					m_subsystemCellChangeQueue.QueueCellChange(x, y - 1, z, value4);
				}
			}
		}
		else
		{
			int value5 = Terrain.ReplaceData(value, RyeBlock.SetIsWild(data, isWild: true));
			m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value5);
		}
	}

	private void GrowCotton(int value, int x, int y, int z, int pollPass)
	{
		if (Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) < 9)
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		int size = CottonBlock.GetSize(data);
		if (size >= 2)
		{
			return;
		}
		if (CottonBlock.GetIsWild(data))
		{
			if (size < 2)
			{
				int data2 = CottonBlock.SetSize(CottonBlock.SetIsWild(data, isWild: true), size + 1);
				int value2 = Terrain.ReplaceData(value, data2);
				m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value2);
			}
			return;
		}
		int cellValueFast = base.SubsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
		if (Terrain.ExtractContents(cellValueFast) == 168)
		{
			int data3 = Terrain.ExtractData(cellValueFast);
			bool hydration = SoilBlock.GetHydration(data3);
			int nitrogen = SoilBlock.GetNitrogen(data3);
			int num = base.SubsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
			int num2 = 8;
			float num3 = 0.8f;
			if (nitrogen > 0)
			{
				num2 -= 2;
				num3 -= 0.4f;
			}
			if (hydration)
			{
				num2 -= 2;
				num3 -= 0.4f;
			}
			if (num <= 4)
			{
				num2 += 8;
			}
			if (pollPass % MathUtils.Max(num2, 1) == 0 || !(num3 < 1f))
			{
				int data4 = CottonBlock.SetSize(data, MathUtils.Min(size + 1, 2));
				if (m_random.Float(0f, 1f) < num3 && size == 1)
				{
					data4 = CottonBlock.SetIsWild(data4, isWild: true);
				}
				int value3 = Terrain.ReplaceData(value, data4);
				m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value3);
				if (size + 1 == 2)
				{
					int data5 = SoilBlock.SetNitrogen(data3, MathUtils.Max(nitrogen - 1, 0));
					int value4 = Terrain.ReplaceData(cellValueFast, data5);
					m_subsystemCellChangeQueue.QueueCellChange(x, y - 1, z, value4);
				}
			}
		}
		else
		{
			int value5 = Terrain.ReplaceData(value, CottonBlock.SetIsWild(data, isWild: true));
			m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value5);
		}
	}

	private void GrowPumpkin(int value, int x, int y, int z, int pollPass)
	{
		if (Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) < 9)
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		int size = BasePumpkinBlock.GetSize(data);
		if (BasePumpkinBlock.GetIsDead(data) || size >= 7)
		{
			return;
		}
		int cellValueFast = base.SubsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
		int num = Terrain.ExtractContents(cellValueFast);
		int data2 = Terrain.ExtractData(cellValueFast);
		bool num2 = num == 168 && SoilBlock.GetHydration(data2);
		int num3 = ((num == 168) ? SoilBlock.GetNitrogen(data2) : 0);
		int num4 = base.SubsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
		int num5 = 4;
		float num6 = 0.15f;
		if (num == 168)
		{
			num5--;
			num6 -= 0.05f;
		}
		if (num3 > 0)
		{
			num5--;
			num6 -= 0.05f;
		}
		if (num2)
		{
			num5--;
			num6 -= 0.05f;
		}
		if (num4 <= 4)
		{
			num5 += 5;
		}
		if (pollPass % MathUtils.Max(num5, 1) == 0 || !(num6 < 1f))
		{
			int data3 = BasePumpkinBlock.SetSize(data, MathUtils.Min(size + 1, 7));
			if (m_random.Float(0f, 1f) < num6)
			{
				data3 = BasePumpkinBlock.SetIsDead(data3, isDead: true);
			}
			int value2 = Terrain.ReplaceData(value, data3);
			m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value2);
			if (num == 168 && size + 1 == 7)
			{
				int data4 = SoilBlock.SetNitrogen(data2, MathUtils.Max(num3 - 3, 0));
				int value3 = Terrain.ReplaceData(cellValueFast, data4);
				m_subsystemCellChangeQueue.QueueCellChange(x, y - 1, z, value3);
			}
		}
	}

	private void Lifecycle(int value, int x, int y, int z, bool applyImmediately)
	{
	}
}
