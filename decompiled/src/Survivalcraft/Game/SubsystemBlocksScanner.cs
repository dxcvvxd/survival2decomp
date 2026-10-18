using System;
using System.Linq;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemBlocksScanner : Subsystem, IUpdateable
{
	public const float ScanPeriod = 60f;

	private SubsystemPollableBlockBehavior[][] m_pollableBehaviorsByContents;

	private Point2 m_pollChunkCoordinates;

	private int m_pollX;

	private int m_pollZ;

	private int m_pollPass;

	private float m_pollShaftsCount;

	private SubsystemTime m_subsystemTime;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	public UpdateOrder UpdateOrder => UpdateOrder.BlocksScanner;

	public event Action<TerrainChunk> ScanningChunkCompleted;

	public void Update(float dt)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		m_pollShaftsCount += (float)(terrain.AllocatedChunks.Length * 16 * 16) * dt / 60f;
		if (m_subsystemTime.GameTimeFactor <= 1f)
		{
			m_pollShaftsCount = MathUtils.Clamp(m_pollShaftsCount, 0f, 500f);
		}
		TerrainChunk terrainChunk = terrain.LoopChunks(m_pollChunkCoordinates.X, m_pollChunkCoordinates.Y, skipStartChunk: false);
		if (terrainChunk == null)
		{
			return;
		}
		while (m_pollShaftsCount >= 1f)
		{
			if (terrainChunk.State <= TerrainChunkState.InvalidContents4)
			{
				m_pollShaftsCount -= 256f;
			}
			else
			{
				while (m_pollX < 16)
				{
					while (m_pollZ < 16)
					{
						if (m_pollShaftsCount < 1f)
						{
							return;
						}
						m_pollShaftsCount--;
						int topHeightFast = terrainChunk.GetTopHeightFast(m_pollX, m_pollZ);
						int num = TerrainChunk.CalculateCellIndex(m_pollX, 0, m_pollZ);
						int num2 = 0;
						while (num2 <= topHeightFast)
						{
							int cellValueFast = terrainChunk.GetCellValueFast(num);
							int num3 = Terrain.ExtractContents(cellValueFast);
							if (num3 != 0)
							{
								SubsystemPollableBlockBehavior[] array = m_pollableBehaviorsByContents[num3];
								for (int i = 0; i < array.Length; i++)
								{
									array[i].OnPoll(cellValueFast, terrainChunk.Origin.X + m_pollX, num2, terrainChunk.Origin.Y + m_pollZ, m_pollPass);
								}
							}
							num2++;
							num++;
						}
						m_pollZ++;
					}
					m_pollZ = 0;
					m_pollX++;
				}
				m_pollX = 0;
			}
			ScanningChunkCompleted?.Invoke(terrainChunk);
			terrainChunk = terrain.LoopChunks(terrainChunk.Coords.X, terrainChunk.Coords.Y, skipStartChunk: true, out var hasLooped);
			if (terrainChunk == null)
			{
				break;
			}
			if (hasLooped)
			{
				m_pollPass++;
			}
			m_pollChunkCoordinates = terrainChunk.Coords;
		}
	}

	protected override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_pollChunkCoordinates = valuesDictionary.GetValue<Point2>("PollChunkCoordinates");
		Point2 value = valuesDictionary.GetValue<Point2>("PollPoint");
		m_pollX = value.X;
		m_pollZ = value.Y;
		m_pollPass = valuesDictionary.GetValue<int>("PollPass");
		m_pollableBehaviorsByContents = new SubsystemPollableBlockBehavior[BlocksManager.Blocks.Length][];
		for (int i = 0; i < m_pollableBehaviorsByContents.Length; i++)
		{
			m_pollableBehaviorsByContents[i] = (from s in m_subsystemBlockBehaviors.GetBlockBehaviors(i)
				where s is SubsystemPollableBlockBehavior
				select (SubsystemPollableBlockBehavior)s).ToArray();
		}
	}

	protected override void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("PollChunkCoordinates", m_pollChunkCoordinates);
		valuesDictionary.SetValue("PollPoint", new Point2(m_pollX, m_pollZ));
		valuesDictionary.SetValue("PollPass", m_pollPass);
	}
}
