using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Engine;

namespace Game;

public class TerrainUpdater
{
	private class UpdateStatistics
	{
		private static int m_counter;

		public double FindBestChunkTime;

		public int FindBestChunkCount;

		public double LoadingTime;

		public int LoadingCount;

		public double ContentsTime1;

		public int ContentsCount1;

		public double ContentsTime2;

		public int ContentsCount2;

		public double ContentsTime3;

		public int ContentsCount3;

		public double ContentsTime4;

		public int ContentsCount4;

		public double LightTime;

		public int LightCount;

		public double LightSourcesTime;

		public int LightSourcesCount;

		public double LightPropagateTime;

		public int LightPropagateCount;

		public int LightSourceInstancesCount;

		public double VerticesTime1;

		public int VerticesCount1;

		public double VerticesTime2;

		public int VerticesCount2;

		public int HashCount;

		public double HashTime;

		public int GeneratedSlices;

		public int SkippedSlices;

		public void Log()
		{
			Engine.Log.Information("Terrain Update #{0}", m_counter++);
			if (FindBestChunkCount > 0)
			{
				Engine.Log.Information("    FindBestChunk:          {0:0.0}ms ({1}x)", FindBestChunkTime * 1000.0, FindBestChunkCount);
			}
			if (LoadingCount > 0)
			{
				Engine.Log.Information("    Loading:                {0:0.0}ms ({1}x)", LoadingTime * 1000.0, LoadingCount);
			}
			if (ContentsCount1 > 0)
			{
				Engine.Log.Information("    Contents1:              {0:0.0}ms ({1}x)", ContentsTime1 * 1000.0, ContentsCount1);
			}
			if (ContentsCount2 > 0)
			{
				Engine.Log.Information("    Contents2:              {0:0.0}ms ({1}x)", ContentsTime2 * 1000.0, ContentsCount2);
			}
			if (ContentsCount3 > 0)
			{
				Engine.Log.Information("    Contents3:              {0:0.0}ms ({1}x)", ContentsTime3 * 1000.0, ContentsCount3);
			}
			if (ContentsCount4 > 0)
			{
				Engine.Log.Information("    Contents4:              {0:0.0}ms ({1}x)", ContentsTime4 * 1000.0, ContentsCount4);
			}
			if (LightCount > 0)
			{
				Engine.Log.Information("    Light:                  {0:0.0}ms ({1}x)", LightTime * 1000.0, LightCount);
			}
			if (LightSourcesCount > 0)
			{
				Engine.Log.Information("    LightSources:           {0:0.0}ms ({1}x)", LightSourcesTime * 1000.0, LightSourcesCount);
			}
			if (LightPropagateCount > 0)
			{
				Engine.Log.Information("    LightPropagate:         {0:0.0}ms ({1}x) {2} ls", LightPropagateTime * 1000.0, LightPropagateCount, LightSourceInstancesCount);
			}
			if (VerticesCount1 > 0)
			{
				Engine.Log.Information("    Vertices1:              {0:0.0}ms ({1}x)", VerticesTime1 * 1000.0, VerticesCount1);
			}
			if (VerticesCount2 > 0)
			{
				Engine.Log.Information("    Vertices2:              {0:0.0}ms ({1}x)", VerticesTime2 * 1000.0, VerticesCount2);
			}
			if (VerticesCount1 + VerticesCount2 > 0)
			{
				Engine.Log.Information("    AllVertices:            {0:0.0}ms ({1}x)", (VerticesTime1 + VerticesTime2) * 1000.0, VerticesCount1 + VerticesCount2);
			}
			if (HashCount > 0)
			{
				Engine.Log.Information("        Hash:               {0:0.0}ms ({1}x)", HashTime * 1000.0, HashCount);
			}
			if (GeneratedSlices > 0)
			{
				Engine.Log.Information("        Generated Slices:   {0}/{1}", GeneratedSlices, GeneratedSlices + SkippedSlices);
			}
		}
	}

	private struct UpdateLocation
	{
		public Vector2 Center;

		public Vector2? LastChunksUpdateCenter;

		public float VisibilityDistance;

		public float ContentDistance;
	}

	private struct UpdateParameters
	{
		public TerrainChunk[] Chunks;

		public Dictionary<int, UpdateLocation> Locations;
	}

	private struct LightSource
	{
		public int X;

		public int Y;

		public int Z;

		public int Light;
	}

	private FloatCurve TemperatureCurve = new FloatCurve(new Vector2(0f, 0f), new Vector2(0.125f, 0f), new Vector2(0.25f, 0f), new Vector2(0.375f, -4f), new Vector2(0.5f, -12f), new Vector2(0.625f, -24f), new Vector2(0.75f, -12f), new Vector2(0.875f, -4f), new Vector2(1f, 0f));

	private FloatCurve HumidityCurve = new FloatCurve(new Vector2(0f, 0f), new Vector2(0.25f, 0f), new Vector2(0.5f, 0f), new Vector2(0.75f, 0f), new Vector2(1f, 0f));

	private const int m_lightAttenuationWithDistance = 1;

	private const float m_updateHysteresis = 8f;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemGameInfo m_subsystemGameInfo;

	private SubsystemSky m_subsystemSky;

	private SubsystemSeasons m_subsystemSeasons;

	private SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	private Terrain m_terrain;

	private DynamicArray<LightSource> m_lightSources = new DynamicArray<LightSource>();

	private UpdateStatistics m_statistics = new UpdateStatistics();

	private Task m_task;

	private AutoResetEvent m_updateEvent = new AutoResetEvent(initialState: true);

	private ManualResetEvent m_pauseEvent = new ManualResetEvent(initialState: true);

	private volatile bool m_quitUpdateThread;

	private bool m_unpauseUpdateThread;

	private object m_updateParametersLock = new object();

	private object m_unpauseLock = new object();

	private UpdateParameters m_updateParameters;

	private UpdateParameters m_threadUpdateParameters;

	private int m_lastSkylightValue;

	private int m_synchronousUpdateFrame;

	private Dictionary<int, UpdateLocation?> m_pendingLocations = new Dictionary<int, UpdateLocation?>();

	public static int ChunkUpdates;

	public static int SlowTerrainUpdate;

	public static bool LogTerrainUpdateStats;

	public AutoResetEvent UpdateEvent => m_updateEvent;

	public event Action<TerrainChunk> ChunkInitialized;

	public TerrainUpdater(SubsystemTerrain subsystemTerrain)
	{
		ChunkUpdates = 0;
		m_subsystemTerrain = subsystemTerrain;
		m_subsystemGameInfo = m_subsystemTerrain.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSky = m_subsystemTerrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemSeasons = m_subsystemTerrain.Project.FindSubsystem<SubsystemSeasons>(throwOnError: true);
		m_subsystemBlockBehaviors = m_subsystemTerrain.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_terrain = subsystemTerrain.Terrain;
		m_updateParameters.Chunks = new TerrainChunk[0];
		m_updateParameters.Locations = new Dictionary<int, UpdateLocation>();
		m_threadUpdateParameters.Chunks = new TerrainChunk[0];
		m_threadUpdateParameters.Locations = new Dictionary<int, UpdateLocation>();
		SettingsManager.SettingChanged += SettingsManager_SettingChanged;
	}

	public void Dispose()
	{
		SettingsManager.SettingChanged -= SettingsManager_SettingChanged;
		m_quitUpdateThread = true;
		UnpauseUpdateThread();
		m_updateEvent.Set();
		if (m_task != null)
		{
			m_task.Wait();
			m_task = null;
		}
		m_pauseEvent.Dispose();
		m_updateEvent.Dispose();
	}

	public void RequestSynchronousUpdate()
	{
		m_synchronousUpdateFrame = Time.FrameIndex;
	}

	public void SetUpdateLocation(int locationIndex, Vector2 center, float visibilityDistance, float contentDistance)
	{
		contentDistance = MathUtils.Max(contentDistance, visibilityDistance);
		m_updateParameters.Locations.TryGetValue(locationIndex, out var value);
		if (contentDistance != value.ContentDistance || visibilityDistance != value.VisibilityDistance || !value.LastChunksUpdateCenter.HasValue || Vector2.DistanceSquared(center, value.LastChunksUpdateCenter.Value) > 64f)
		{
			value.Center = center;
			value.VisibilityDistance = visibilityDistance;
			value.ContentDistance = contentDistance;
			value.LastChunksUpdateCenter = center;
			m_pendingLocations[locationIndex] = value;
		}
	}

	public void RemoveUpdateLocation(int locationIndex)
	{
		m_pendingLocations[locationIndex] = null;
	}

	public float GetUpdateProgress(int locationIndex, float visibilityDistance, float contentDistance)
	{
		int num = 0;
		int num2 = 0;
		if (m_updateParameters.Locations.TryGetValue(locationIndex, out var value))
		{
			visibilityDistance = MathUtils.Max(MathUtils.Min(visibilityDistance, value.VisibilityDistance) - 8f - 0.1f, 0f);
			contentDistance = MathUtils.Max(MathUtils.Min(contentDistance, value.ContentDistance) - 8f - 0.1f, 0f);
			float num3 = MathUtils.Sqr(visibilityDistance);
			float num4 = MathUtils.Sqr(contentDistance);
			float v = MathUtils.Max(visibilityDistance, contentDistance);
			Point2 point = Terrain.ToChunk(value.Center - new Vector2(v));
			Point2 point2 = Terrain.ToChunk(value.Center + new Vector2(v));
			for (int i = point.X; i <= point2.X; i++)
			{
				for (int j = point.Y; j <= point2.Y; j++)
				{
					TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(i, j);
					float num5 = Vector2.DistanceSquared(v2: new Vector2(((float)i + 0.5f) * 16f, ((float)j + 0.5f) * 16f), v1: value.Center);
					if (num5 <= num3)
					{
						if (chunkAtCoords == null || chunkAtCoords.State < TerrainChunkState.Valid)
						{
							num2++;
						}
						else
						{
							num++;
						}
					}
					else if (num5 <= num4)
					{
						if (chunkAtCoords == null || chunkAtCoords.State < TerrainChunkState.InvalidLight)
						{
							num2++;
						}
						else
						{
							num++;
						}
					}
				}
			}
			if (num2 <= 0)
			{
				return 1f;
			}
			return (float)num / (float)(num2 + num);
		}
		return 0f;
	}

	public void Update()
	{
		if (m_subsystemSky.SkyLightValue != m_lastSkylightValue)
		{
			m_lastSkylightValue = m_subsystemSky.SkyLightValue;
			DowngradeAllChunksState(TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
		}
		int num = (int)MathUtils.Round(TemperatureCurve.Sample(m_subsystemGameInfo.WorldSettings.TimeOfYear));
		int num2 = (int)MathUtils.Round(HumidityCurve.Sample(m_subsystemGameInfo.WorldSettings.TimeOfYear));
		if (num != m_terrain.SeasonTemperature || num2 != m_terrain.SeasonHumidity)
		{
			m_terrain.SeasonTemperature = num;
			m_terrain.SeasonHumidity = num2;
			DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: false);
		}
		if (!SettingsManager.MultithreadedTerrainUpdate)
		{
			if (m_task != null)
			{
				m_quitUpdateThread = true;
				UnpauseUpdateThread();
				m_updateEvent.Set();
				m_task.Wait();
				m_task = null;
			}
			double realTime = Time.RealTime;
			while (!SynchronousUpdateFunction() && Time.RealTime - realTime < 0.009999999776482582)
			{
			}
		}
		else if (m_task == null)
		{
			m_quitUpdateThread = false;
			m_task = Task.Run((Action)ThreadUpdateFunction);
			UnpauseUpdateThread();
			m_updateEvent.Set();
		}
		if (m_pendingLocations.Count > 0)
		{
			m_pauseEvent.Reset();
			if (m_updateEvent.WaitOne(0))
			{
				m_pauseEvent.Set();
				try
				{
					foreach (KeyValuePair<int, UpdateLocation?> pendingLocation in m_pendingLocations)
					{
						if (pendingLocation.Value.HasValue)
						{
							m_updateParameters.Locations[pendingLocation.Key] = pendingLocation.Value.Value;
						}
						else
						{
							m_updateParameters.Locations.Remove(pendingLocation.Key);
						}
					}
					if (AllocateAndFreeChunks(m_updateParameters.Locations.Values.ToArray()))
					{
						m_updateParameters.Chunks = m_terrain.AllocatedChunks;
					}
					m_pendingLocations.Clear();
				}
				finally
				{
					m_updateEvent.Set();
				}
			}
		}
		if (Monitor.TryEnter(m_updateParametersLock, 0))
		{
			try
			{
				if (SendReceiveChunkStates())
				{
					UnpauseUpdateThread();
				}
			}
			finally
			{
				Monitor.Exit(m_updateParametersLock);
			}
		}
		TerrainChunk[] allocatedChunks = m_terrain.AllocatedChunks;
		foreach (TerrainChunk terrainChunk in allocatedChunks)
		{
			if (terrainChunk.State >= TerrainChunkState.InvalidVertices1 && !terrainChunk.AreBehaviorsNotified)
			{
				terrainChunk.AreBehaviorsNotified = true;
				NotifyBlockBehaviors(terrainChunk);
			}
		}
	}

	public void PrepareForDrawing(Camera camera)
	{
		SetUpdateLocation(camera.GameWidget.PlayerData.PlayerIndex, camera.ViewPosition.XZ, m_subsystemSky.VisibilityRange, 64f);
		if (m_synchronousUpdateFrame != Time.FrameIndex)
		{
			return;
		}
		List<TerrainChunk> list = DetermineSynchronousUpdateChunks(camera.ViewPosition, camera.ViewDirection);
		if (list.Count <= 0)
		{
			return;
		}
		m_updateEvent.WaitOne();
		try
		{
			SendReceiveChunkStates();
			SendReceiveChunkStatesThread();
			foreach (TerrainChunk item in list)
			{
				while (item.ThreadState < TerrainChunkState.Valid)
				{
					UpdateChunkSingleStep(item, m_subsystemSky.SkyLightValue);
				}
			}
			SendReceiveChunkStatesThread();
			SendReceiveChunkStates();
		}
		finally
		{
			m_updateEvent.Set();
		}
	}

	public void DowngradeChunkNeighborhoodState(Point2 coordinates, int radius, TerrainChunkState state, bool forceGeometryRegeneration)
	{
		for (int i = -radius; i <= radius; i++)
		{
			for (int j = -radius; j <= radius; j++)
			{
				TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(coordinates.X + i, coordinates.Y + j);
				if (chunkAtCoords == null)
				{
					continue;
				}
				if (chunkAtCoords.State > state)
				{
					chunkAtCoords.State = state;
					if (forceGeometryRegeneration)
					{
						chunkAtCoords.Geometry.InvalidateSlicesGeometryHashes();
					}
				}
				chunkAtCoords.WasDowngraded = true;
			}
		}
	}

	public void DowngradeAllChunksState(TerrainChunkState state, bool forceGeometryRegeneration)
	{
		TerrainChunk[] allocatedChunks = m_terrain.AllocatedChunks;
		foreach (TerrainChunk terrainChunk in allocatedChunks)
		{
			if (terrainChunk.State > state)
			{
				terrainChunk.State = state;
				if (forceGeometryRegeneration)
				{
					terrainChunk.Geometry.InvalidateSlicesGeometryHashes();
				}
			}
			terrainChunk.WasDowngraded = true;
		}
	}

	private static bool IsChunkInRange(Vector2 chunkCenter, ref UpdateLocation location)
	{
		return Vector2.DistanceSquared(location.Center, chunkCenter) <= MathUtils.Sqr(location.ContentDistance);
	}

	private static bool IsChunkInRange(Vector2 chunkCenter, UpdateLocation[] locations)
	{
		for (int i = 0; i < locations.Length; i++)
		{
			if (IsChunkInRange(chunkCenter, ref locations[i]))
			{
				return true;
			}
		}
		return false;
	}

	private bool AllocateAndFreeChunks(UpdateLocation[] locations)
	{
		bool result = false;
		TerrainChunk[] allocatedChunks = m_terrain.AllocatedChunks;
		foreach (TerrainChunk terrainChunk in allocatedChunks)
		{
			if (IsChunkInRange(terrainChunk.Center, locations))
			{
				continue;
			}
			result = true;
			foreach (SubsystemBlockBehavior blockBehavior in m_subsystemBlockBehaviors.BlockBehaviors)
			{
				blockBehavior.OnChunkDiscarding(terrainChunk);
			}
			m_subsystemTerrain.TerrainSerializer.SaveChunk(terrainChunk);
			m_terrain.FreeChunk(terrainChunk);
		}
		for (int j = 0; j < locations.Length; j++)
		{
			Point2 point = Terrain.ToChunk(locations[j].Center - new Vector2(locations[j].ContentDistance));
			Point2 point2 = Terrain.ToChunk(locations[j].Center + new Vector2(locations[j].ContentDistance));
			for (int k = point.X; k <= point2.X; k++)
			{
				for (int l = point.Y; l <= point2.Y; l++)
				{
					Vector2 chunkCenter = new Vector2(((float)k + 0.5f) * 16f, ((float)l + 0.5f) * 16f);
					TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(k, l);
					if (chunkAtCoords == null)
					{
						if (IsChunkInRange(chunkCenter, ref locations[j]))
						{
							result = true;
							m_terrain.AllocateChunk(k, l);
							DowngradeChunkNeighborhoodState(new Point2(k, l), 0, TerrainChunkState.NotLoaded, forceGeometryRegeneration: false);
							DowngradeChunkNeighborhoodState(new Point2(k, l), 1, TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
						}
					}
					else if (chunkAtCoords.Coords.X != k || chunkAtCoords.Coords.Y != l)
					{
						Log.Error("Chunk wraparound detected at {0}", chunkAtCoords.Coords);
					}
				}
			}
		}
		return result;
	}

	private bool SendReceiveChunkStates()
	{
		bool result = false;
		TerrainChunk[] chunks = m_updateParameters.Chunks;
		foreach (TerrainChunk terrainChunk in chunks)
		{
			if (terrainChunk.WasDowngraded)
			{
				terrainChunk.DowngradedState = terrainChunk.State;
				terrainChunk.WasDowngraded = false;
				result = true;
			}
			else if (terrainChunk.UpgradedState.HasValue)
			{
				terrainChunk.State = terrainChunk.UpgradedState.Value;
			}
			terrainChunk.UpgradedState = null;
		}
		return result;
	}

	private void SendReceiveChunkStatesThread()
	{
		TerrainChunk[] chunks = m_threadUpdateParameters.Chunks;
		foreach (TerrainChunk terrainChunk in chunks)
		{
			if (terrainChunk.DowngradedState.HasValue)
			{
				terrainChunk.ThreadState = terrainChunk.DowngradedState.Value;
				terrainChunk.DowngradedState = null;
			}
			else if (terrainChunk.WasUpgraded)
			{
				terrainChunk.UpgradedState = terrainChunk.ThreadState;
			}
			terrainChunk.WasUpgraded = false;
		}
	}

	private void ThreadUpdateFunction()
	{
		while (!m_quitUpdateThread)
		{
			m_pauseEvent.WaitOne();
			m_updateEvent.WaitOne();
			try
			{
				if (!SynchronousUpdateFunction())
				{
					continue;
				}
				lock (m_unpauseLock)
				{
					if (!m_unpauseUpdateThread)
					{
						m_pauseEvent.Reset();
					}
					m_unpauseUpdateThread = false;
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				m_updateEvent.Set();
			}
		}
	}

	private bool SynchronousUpdateFunction()
	{
		lock (m_updateParametersLock)
		{
			m_threadUpdateParameters = m_updateParameters;
			SendReceiveChunkStatesThread();
		}
		TerrainChunk terrainChunk = FindBestChunkToUpdate(out var desiredState);
		if (terrainChunk != null)
		{
			double realTime = Time.RealTime;
			do
			{
				UpdateChunkSingleStep(terrainChunk, m_subsystemSky.SkyLightValue);
			}
			while (terrainChunk.ThreadState < desiredState && Time.RealTime - realTime < 0.009999999776482582);
			return false;
		}
		if (LogTerrainUpdateStats)
		{
			m_statistics.Log();
			m_statistics = new UpdateStatistics();
		}
		return true;
	}

	private TerrainChunk FindBestChunkToUpdate(out TerrainChunkState desiredState)
	{
		double realTime = Time.RealTime;
		TerrainChunk[] chunks = m_threadUpdateParameters.Chunks;
		UpdateLocation[] array = m_threadUpdateParameters.Locations.Values.ToArray();
		float num = float.MaxValue;
		TerrainChunk result = null;
		desiredState = TerrainChunkState.NotLoaded;
		foreach (TerrainChunk terrainChunk in chunks)
		{
			if (terrainChunk.ThreadState >= TerrainChunkState.Valid)
			{
				continue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				float num2 = Vector2.DistanceSquared(array[j].Center, terrainChunk.Center);
				if (num2 < num)
				{
					if (num2 <= MathUtils.Sqr(array[j].VisibilityDistance))
					{
						desiredState = TerrainChunkState.Valid;
						num = num2;
						result = terrainChunk;
					}
					else if (terrainChunk.ThreadState < TerrainChunkState.InvalidVertices1 && num2 <= MathUtils.Sqr(array[j].ContentDistance))
					{
						desiredState = TerrainChunkState.InvalidVertices1;
						num = num2;
						result = terrainChunk;
					}
				}
			}
		}
		double realTime2 = Time.RealTime;
		m_statistics.FindBestChunkTime += realTime2 - realTime;
		m_statistics.FindBestChunkCount++;
		return result;
	}

	private List<TerrainChunk> DetermineSynchronousUpdateChunks(Vector3 viewPosition, Vector3 viewDirection)
	{
		Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(viewDirection, vector));
		Vector3[] obj = new Vector3[6]
		{
			viewPosition,
			viewPosition + 6f * viewDirection,
			viewPosition + 6f * viewDirection - 6f * vector,
			viewPosition + 6f * viewDirection + 6f * vector,
			viewPosition + 6f * viewDirection - 2f * vector2,
			viewPosition + 6f * viewDirection + 2f * vector2
		};
		List<TerrainChunk> list = new List<TerrainChunk>();
		Vector3[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 vector3 = array[i];
			TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(Terrain.ToCell(vector3.X), Terrain.ToCell(vector3.Z));
			if (chunkAtCell != null && chunkAtCell.State < TerrainChunkState.Valid && !list.Contains(chunkAtCell))
			{
				list.Add(chunkAtCell);
			}
		}
		return list;
	}

	private void UpdateChunkSingleStep(TerrainChunk chunk, int skylightValue)
	{
		switch (chunk.ThreadState)
		{
		case TerrainChunkState.NotLoaded:
		{
			double realTime19 = Time.RealTime;
			if (m_subsystemTerrain.TerrainSerializer.LoadChunk(chunk))
			{
				chunk.ThreadState = TerrainChunkState.InvalidLight;
				chunk.WasUpgraded = true;
				double realTime20 = Time.RealTime;
				chunk.IsLoaded = true;
				m_statistics.LoadingCount++;
				m_statistics.LoadingTime += realTime20 - realTime19;
			}
			else
			{
				chunk.ThreadState = TerrainChunkState.InvalidContents1;
				chunk.WasUpgraded = true;
			}
			break;
		}
		case TerrainChunkState.InvalidContents1:
		{
			double realTime17 = Time.RealTime;
			m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass1(chunk);
			chunk.ThreadState = TerrainChunkState.InvalidContents2;
			chunk.WasUpgraded = true;
			double realTime18 = Time.RealTime;
			m_statistics.ContentsCount1++;
			m_statistics.ContentsTime1 += realTime18 - realTime17;
			break;
		}
		case TerrainChunkState.InvalidContents2:
		{
			double realTime11 = Time.RealTime;
			m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass2(chunk);
			chunk.ThreadState = TerrainChunkState.InvalidContents3;
			chunk.WasUpgraded = true;
			double realTime12 = Time.RealTime;
			m_statistics.ContentsCount2++;
			m_statistics.ContentsTime2 += realTime12 - realTime11;
			break;
		}
		case TerrainChunkState.InvalidContents3:
		{
			double realTime9 = Time.RealTime;
			m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass3(chunk);
			chunk.ThreadState = TerrainChunkState.InvalidContents4;
			chunk.WasUpgraded = true;
			double realTime10 = Time.RealTime;
			m_statistics.ContentsCount3++;
			m_statistics.ContentsTime3 += realTime10 - realTime9;
			break;
		}
		case TerrainChunkState.InvalidContents4:
		{
			double realTime7 = Time.RealTime;
			m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass4(chunk);
			chunk.ThreadState = TerrainChunkState.InvalidLight;
			chunk.WasUpgraded = true;
			double realTime8 = Time.RealTime;
			m_statistics.ContentsCount4++;
			m_statistics.ContentsTime4 += realTime8 - realTime7;
			break;
		}
		case TerrainChunkState.InvalidLight:
		{
			double realTime3 = Time.RealTime;
			GenerateChunkSunlightAndHeights(chunk, skylightValue);
			chunk.ThreadState = TerrainChunkState.InvalidPropagatedLight;
			chunk.WasUpgraded = true;
			double realTime4 = Time.RealTime;
			m_statistics.LightCount++;
			m_statistics.LightTime += realTime4 - realTime3;
			break;
		}
		case TerrainChunkState.InvalidPropagatedLight:
		{
			for (int k = -1; k <= 1; k++)
			{
				for (int l = -1; l <= 1; l++)
				{
					TerrainChunk chunkAtCoords2 = m_terrain.GetChunkAtCoords(chunk.Coords.X + k, chunk.Coords.Y + l);
					if (chunkAtCoords2 != null && chunkAtCoords2.ThreadState < TerrainChunkState.InvalidPropagatedLight)
					{
						UpdateChunkSingleStep(chunkAtCoords2, skylightValue);
						return;
					}
				}
			}
			double realTime13 = Time.RealTime;
			m_lightSources.Count = 0;
			GenerateChunkLightSources(chunk);
			GenerateChunkEdgeLightSources(chunk, 0);
			GenerateChunkEdgeLightSources(chunk, 1);
			GenerateChunkEdgeLightSources(chunk, 2);
			GenerateChunkEdgeLightSources(chunk, 3);
			double realTime14 = Time.RealTime;
			m_statistics.LightSourcesCount++;
			m_statistics.LightSourcesTime += realTime14 - realTime13;
			double realTime15 = Time.RealTime;
			PropagateLightSources();
			chunk.ThreadState = TerrainChunkState.InvalidVertices1;
			chunk.WasUpgraded = true;
			double realTime16 = Time.RealTime;
			m_statistics.LightPropagateCount++;
			m_statistics.LightSourceInstancesCount += m_lightSources.Count;
			m_statistics.LightPropagateTime += realTime16 - realTime15;
			break;
		}
		case TerrainChunkState.InvalidVertices1:
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(chunk.Coords.X + i, chunk.Coords.Y + j);
					if (chunkAtCoords != null && chunkAtCoords.ThreadState < TerrainChunkState.InvalidVertices1)
					{
						UpdateChunkSingleStep(chunkAtCoords, skylightValue);
						return;
					}
				}
			}
			CalculateChunkSliceContentsHashes(chunk);
			double realTime5 = Time.RealTime;
			lock (chunk.Geometry)
			{
				chunk.NewGeometryData = false;
				GenerateChunkVertices(chunk, 0);
			}
			chunk.ThreadState = TerrainChunkState.InvalidVertices2;
			chunk.WasUpgraded = true;
			double realTime6 = Time.RealTime;
			m_statistics.VerticesCount1++;
			m_statistics.VerticesTime1 += realTime6 - realTime5;
			break;
		}
		case TerrainChunkState.InvalidVertices2:
		{
			double realTime = Time.RealTime;
			lock (chunk.Geometry)
			{
				GenerateChunkVertices(chunk, 1);
				chunk.NewGeometryData = true;
			}
			chunk.ThreadState = TerrainChunkState.Valid;
			chunk.WasUpgraded = true;
			double realTime2 = Time.RealTime;
			ChunkUpdates++;
			m_statistics.VerticesCount2++;
			m_statistics.VerticesTime2 += realTime2 - realTime;
			break;
		}
		}
	}

	private void GenerateChunkSunlightAndHeights(TerrainChunk chunk, int skylightValue)
	{
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num = 0;
				int num2 = 255;
				int num3 = TerrainChunk.CalculateCellIndex(i, 255, j);
				while (num2 >= 0)
				{
					int cellValueFast = chunk.GetCellValueFast(num3);
					if (Terrain.ExtractContents(cellValueFast) != 0)
					{
						num = num2;
						break;
					}
					cellValueFast = Terrain.ReplaceLight(cellValueFast, skylightValue);
					chunk.SetCellValueFast(num3, cellValueFast);
					num2--;
					num3--;
				}
				int num4 = 255;
				num2 = 0;
				num3 = TerrainChunk.CalculateCellIndex(i, 0, j);
				while (num2 <= num + 1)
				{
					int cellValueFast2 = chunk.GetCellValueFast(num3);
					int num5 = Terrain.ExtractContents(cellValueFast2);
					if (BlocksManager.Blocks[num5].IsTransparent)
					{
						num4 = num2;
						break;
					}
					cellValueFast2 = Terrain.ReplaceLight(cellValueFast2, 0);
					chunk.SetCellValueFast(num3, cellValueFast2);
					num2++;
					num3++;
				}
				int num6 = skylightValue;
				num2 = num;
				num3 = TerrainChunk.CalculateCellIndex(i, num, j);
				if (num6 > 0)
				{
					while (num2 >= num4)
					{
						int cellValueFast3 = chunk.GetCellValueFast(num3);
						int num7 = Terrain.ExtractContents(cellValueFast3);
						if (num7 != 0)
						{
							Block block = BlocksManager.Blocks[num7];
							if (!block.IsTransparent || block.LightAttenuation >= num6)
							{
								break;
							}
							num6 -= block.LightAttenuation;
						}
						cellValueFast3 = Terrain.ReplaceLight(cellValueFast3, num6);
						chunk.SetCellValueFast(num3, cellValueFast3);
						num2--;
						num3--;
					}
				}
				int sunlightHeight = num2 + 1;
				while (num2 >= num4)
				{
					int cellValueFast4 = chunk.GetCellValueFast(num3);
					cellValueFast4 = Terrain.ReplaceLight(cellValueFast4, 0);
					chunk.SetCellValueFast(num3, cellValueFast4);
					num2--;
					num3--;
				}
				chunk.SetTopHeightFast(i, j, num);
				chunk.SetBottomHeightFast(i, j, num4);
				chunk.SetSunlightHeightFast(i, j, sunlightHeight);
			}
		}
	}

	private void GenerateChunkLightSources(TerrainChunk chunk)
	{
		Block[] blocks = BlocksManager.Blocks;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int topHeightFast = chunk.GetTopHeightFast(i, j);
				int bottomHeightFast = chunk.GetBottomHeightFast(i, j);
				int num = i + chunk.Origin.X;
				int num2 = j + chunk.Origin.Y;
				int num3 = bottomHeightFast;
				int num4 = TerrainChunk.CalculateCellIndex(i, bottomHeightFast, j);
				while (num3 <= topHeightFast)
				{
					int cellValueFast = chunk.GetCellValueFast(num4);
					Block block = blocks[Terrain.ExtractContents(cellValueFast)];
					if (block.DefaultEmittedLightAmount > 0)
					{
						int emittedLightAmount = block.GetEmittedLightAmount(cellValueFast);
						if (emittedLightAmount > Terrain.ExtractLight(cellValueFast))
						{
							chunk.SetCellValueFast(num4, Terrain.ReplaceLight(cellValueFast, emittedLightAmount));
							if (emittedLightAmount > 1)
							{
								m_lightSources.Add(new LightSource
								{
									X = num,
									Y = num3,
									Z = num2,
									Light = emittedLightAmount
								});
							}
						}
					}
					num3++;
					num4++;
				}
				TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(num - 1, num2);
				TerrainChunk chunkAtCell2 = m_terrain.GetChunkAtCell(num + 1, num2);
				TerrainChunk chunkAtCell3 = m_terrain.GetChunkAtCell(num, num2 - 1);
				TerrainChunk chunkAtCell4 = m_terrain.GetChunkAtCell(num, num2 + 1);
				if (chunkAtCell == null || chunkAtCell2 == null || chunkAtCell3 == null || chunkAtCell4 == null)
				{
					continue;
				}
				int x = num - 1 - chunkAtCell.Origin.X;
				int z = num2 - chunkAtCell.Origin.Y;
				int x2 = num + 1 - chunkAtCell2.Origin.X;
				int z2 = num2 - chunkAtCell2.Origin.Y;
				int x3 = num - chunkAtCell3.Origin.X;
				int z3 = num2 - 1 - chunkAtCell3.Origin.Y;
				int x4 = num - chunkAtCell4.Origin.X;
				int z4 = num2 + 1 - chunkAtCell4.Origin.Y;
				int x5 = Terrain.ExtractSunlightHeight(chunkAtCell.GetShaftValueFast(x, z));
				int x6 = Terrain.ExtractSunlightHeight(chunkAtCell2.GetShaftValueFast(x2, z2));
				int x7 = Terrain.ExtractSunlightHeight(chunkAtCell3.GetShaftValueFast(x3, z3));
				int x8 = Terrain.ExtractSunlightHeight(chunkAtCell4.GetShaftValueFast(x4, z4));
				int num5 = MathUtils.Min(x5, x6, x7, x8);
				int num6 = num5;
				int num7 = TerrainChunk.CalculateCellIndex(i, num5, j);
				while (num6 <= topHeightFast)
				{
					int cellValueFast2 = chunk.GetCellValueFast(num7);
					Block block2 = blocks[Terrain.ExtractContents(cellValueFast2)];
					if (block2.IsTransparent)
					{
						int cellLightFast = chunkAtCell.GetCellLightFast(x, num6, z);
						int cellLightFast2 = chunkAtCell2.GetCellLightFast(x2, num6, z2);
						int cellLightFast3 = chunkAtCell3.GetCellLightFast(x3, num6, z3);
						int cellLightFast4 = chunkAtCell4.GetCellLightFast(x4, num6, z4);
						int num8 = MathUtils.Max(cellLightFast, cellLightFast2, cellLightFast3, cellLightFast4) - 1 - block2.LightAttenuation;
						if (num8 > Terrain.ExtractLight(cellValueFast2))
						{
							chunk.SetCellValueFast(num7, Terrain.ReplaceLight(cellValueFast2, num8));
							if (num8 > 1)
							{
								m_lightSources.Add(new LightSource
								{
									X = num,
									Y = num6,
									Z = num2,
									Light = num8
								});
							}
						}
					}
					num6++;
					num7++;
				}
			}
		}
	}

	private void GenerateChunkEdgeLightSources(TerrainChunk chunk, int face)
	{
		Block[] blocks = BlocksManager.Blocks;
		int num = 0;
		int num2 = 0;
		int x = 0;
		int z = 0;
		TerrainChunk chunkAtCoords;
		switch (face)
		{
		case 0:
			chunkAtCoords = chunk.Terrain.GetChunkAtCoords(chunk.Coords.X, chunk.Coords.Y + 1);
			num2 = 15;
			z = 0;
			break;
		case 1:
			chunkAtCoords = chunk.Terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y);
			num = 15;
			x = 0;
			break;
		case 2:
			chunkAtCoords = chunk.Terrain.GetChunkAtCoords(chunk.Coords.X, chunk.Coords.Y - 1);
			num2 = 0;
			z = 15;
			break;
		default:
			chunkAtCoords = chunk.Terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y);
			num = 0;
			x = 15;
			break;
		}
		if (chunkAtCoords == null || chunkAtCoords.ThreadState < TerrainChunkState.InvalidPropagatedLight)
		{
			return;
		}
		for (int i = 0; i < 16; i++)
		{
			switch (face)
			{
			case 0:
				num = i;
				x = i;
				break;
			case 1:
				num2 = i;
				z = i;
				break;
			case 2:
				num = i;
				x = i;
				break;
			default:
				num2 = i;
				z = i;
				break;
			}
			int x2 = num + chunk.Origin.X;
			int z2 = num2 + chunk.Origin.Y;
			int bottomHeightFast = chunk.GetBottomHeightFast(num, num2);
			int num3 = TerrainChunk.CalculateCellIndex(num, 0, num2);
			int num4 = TerrainChunk.CalculateCellIndex(x, 0, z);
			for (int j = bottomHeightFast; j < 256; j++)
			{
				int cellValueFast = chunk.GetCellValueFast(num3 + j);
				int num5 = Terrain.ExtractContents(cellValueFast);
				if (!blocks[num5].IsTransparent)
				{
					continue;
				}
				int num6 = Terrain.ExtractLight(cellValueFast);
				int num7 = Terrain.ExtractLight(chunkAtCoords.GetCellValueFast(num4 + j)) - 1;
				if (num7 > num6)
				{
					chunk.SetCellValueFast(num3 + j, Terrain.ReplaceLight(cellValueFast, num7));
					if (num7 > 1)
					{
						m_lightSources.Add(new LightSource
						{
							X = x2,
							Y = j,
							Z = z2,
							Light = num7
						});
					}
				}
			}
		}
	}

	private void PropagateLightSources()
	{
		for (int i = 0; i < m_lightSources.Count && i < 120000; i++)
		{
			LightSource lightSource = m_lightSources.Array[i];
			int light = lightSource.Light;
			int x = lightSource.X;
			int y = lightSource.Y;
			int z = lightSource.Z;
			int num = x & 0xF;
			int num2 = z & 0xF;
			TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(x, z);
			if (num == 0)
			{
				PropagateLightSource(m_terrain.GetChunkAtCell(x - 1, z), x - 1, y, z, light);
			}
			else
			{
				PropagateLightSource(chunkAtCell, x - 1, y, z, light);
			}
			if (num == 15)
			{
				PropagateLightSource(m_terrain.GetChunkAtCell(x + 1, z), x + 1, y, z, light);
			}
			else
			{
				PropagateLightSource(chunkAtCell, x + 1, y, z, light);
			}
			if (num2 == 0)
			{
				PropagateLightSource(m_terrain.GetChunkAtCell(x, z - 1), x, y, z - 1, light);
			}
			else
			{
				PropagateLightSource(chunkAtCell, x, y, z - 1, light);
			}
			if (num2 == 15)
			{
				PropagateLightSource(m_terrain.GetChunkAtCell(x, z + 1), x, y, z + 1, light);
			}
			else
			{
				PropagateLightSource(chunkAtCell, x, y, z + 1, light);
			}
			if (y > 0)
			{
				PropagateLightSource(chunkAtCell, x, y - 1, z, light);
			}
			if (y < 255)
			{
				PropagateLightSource(chunkAtCell, x, y + 1, z, light);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void PropagateLightSource(TerrainChunk chunk, int x, int y, int z, int light)
	{
		if (chunk == null)
		{
			return;
		}
		int index = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
		int cellValueFast = chunk.GetCellValueFast(index);
		int num = Terrain.ExtractContents(cellValueFast);
		Block block = BlocksManager.Blocks[num];
		if (!block.IsTransparent)
		{
			return;
		}
		int num2 = light - block.LightAttenuation - 1;
		if (num2 > Terrain.ExtractLight(cellValueFast))
		{
			if (num2 > 1)
			{
				m_lightSources.Add(new LightSource
				{
					X = x,
					Y = y,
					Z = z,
					Light = num2
				});
			}
			chunk.SetCellValueFast(index, Terrain.ReplaceLight(cellValueFast, num2));
		}
	}

	private void GenerateChunkVertices(TerrainChunk chunk, int stage)
	{
		m_subsystemTerrain.BlockGeometryGenerator.ResetCache();
		TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y - 1);
		TerrainChunk chunkAtCoords2 = m_terrain.GetChunkAtCoords(chunk.Coords.X, chunk.Coords.Y - 1);
		TerrainChunk chunkAtCoords3 = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y - 1);
		TerrainChunk chunkAtCoords4 = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y);
		TerrainChunk chunkAtCoords5 = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y);
		TerrainChunk chunkAtCoords6 = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y + 1);
		TerrainChunk chunkAtCoords7 = m_terrain.GetChunkAtCoords(chunk.Coords.X, chunk.Coords.Y + 1);
		TerrainChunk chunkAtCoords8 = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y + 1);
		int num = 0;
		int num2 = 0;
		int num3 = 16;
		int num4 = 16;
		if (chunkAtCoords4 == null)
		{
			num++;
		}
		if (chunkAtCoords2 == null)
		{
			num2++;
		}
		if (chunkAtCoords5 == null)
		{
			num3--;
		}
		if (chunkAtCoords7 == null)
		{
			num4--;
		}
		for (int i = 0; i < 16; i++)
		{
			if (i % 2 != stage)
			{
				continue;
			}
			TerrainChunkSliceGeometry terrainChunkSliceGeometry = chunk.Geometry.Slices[i];
			if (terrainChunkSliceGeometry.GeometryHash != 0 && terrainChunkSliceGeometry.GeometryHash == terrainChunkSliceGeometry.ContentsHash)
			{
				m_statistics.SkippedSlices++;
				continue;
			}
			m_statistics.GeneratedSlices++;
			terrainChunkSliceGeometry.GeometryHash = 0;
			TerrainGeometrySubset[] subsets = terrainChunkSliceGeometry.Subsets;
			foreach (TerrainGeometrySubset obj in subsets)
			{
				obj.Vertices.Count = 0;
				obj.Indices.Count = 0;
			}
			for (int k = num; k < num3; k++)
			{
				for (int l = num2; l < num4; l++)
				{
					switch (k)
					{
					case 0:
						if ((l == 0 && chunkAtCoords == null) || (l == 15 && chunkAtCoords6 == null))
						{
							continue;
						}
						break;
					case 15:
						if ((l == 0 && chunkAtCoords3 == null) || (l == 15 && chunkAtCoords8 == null))
						{
							continue;
						}
						break;
					}
					int num5 = k + chunk.Origin.X;
					int num6 = l + chunk.Origin.Y;
					int bottomHeightFast = chunk.GetBottomHeightFast(k, l);
					int bottomHeight = m_terrain.GetBottomHeight(num5 - 1, num6);
					int bottomHeight2 = m_terrain.GetBottomHeight(num5 + 1, num6);
					int bottomHeight3 = m_terrain.GetBottomHeight(num5, num6 - 1);
					int bottomHeight4 = m_terrain.GetBottomHeight(num5, num6 + 1);
					int x = MathUtils.Min(bottomHeightFast - 1, MathUtils.Min(bottomHeight, bottomHeight2, bottomHeight3, bottomHeight4));
					int x2 = chunk.GetTopHeightFast(k, l) + 1;
					int num7 = MathUtils.Max(16 * i, x, 1);
					int num8 = MathUtils.Min(16 * (i + 1), x2, 255);
					int num9 = TerrainChunk.CalculateCellIndex(k, 0, l);
					for (int m = num7; m < num8; m++)
					{
						int cellValueFast = chunk.GetCellValueFast(num9 + m);
						int num10 = Terrain.ExtractContents(cellValueFast);
						if (num10 != 0)
						{
							BlocksManager.Blocks[num10].GenerateTerrainVertices(m_subsystemTerrain.BlockGeometryGenerator, chunk.Geometry.Slices[i], cellValueFast, num5, m, num6);
						}
					}
				}
			}
			terrainChunkSliceGeometry.GeometryHash = terrainChunkSliceGeometry.ContentsHash;
		}
	}

	private void CalculateChunkSliceContentsHashes(TerrainChunk chunk)
	{
		double realTime = Time.RealTime;
		int num = 1;
		num += m_terrain.SeasonTemperature;
		num *= 31;
		num += m_terrain.SeasonHumidity;
		num *= 31;
		TerrainChunkGeometry geometry = chunk.Geometry;
		for (int i = 0; i < 16; i++)
		{
			geometry.Slices[i].ContentsHash = num;
		}
		int num2 = chunk.Origin.X - 1;
		int num3 = chunk.Origin.X + 16 + 1;
		int num4 = chunk.Origin.Y - 1;
		int num5 = chunk.Origin.Y + 16 + 1;
		for (int j = num2; j < num3; j++)
		{
			for (int k = num4; k < num5; k++)
			{
				TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(j, k);
				if (chunkAtCell == null)
				{
					continue;
				}
				int num6 = j & 0xF;
				int num7 = k & 0xF;
				int shaftValueFast = chunkAtCell.GetShaftValueFast(num6, num7);
				int num8 = Terrain.ExtractTopHeight(shaftValueFast);
				int num9 = Terrain.ExtractBottomHeight(shaftValueFast);
				int x = ((num6 > 0) ? chunkAtCell.GetBottomHeightFast(num6 - 1, num7) : m_terrain.GetBottomHeight(j - 1, k));
				int x2 = ((num7 > 0) ? chunkAtCell.GetBottomHeightFast(num6, num7 - 1) : m_terrain.GetBottomHeight(j, k - 1));
				int x3 = ((num6 < 15) ? chunkAtCell.GetBottomHeightFast(num6 + 1, num7) : m_terrain.GetBottomHeight(j + 1, k));
				int x4 = ((num7 < 15) ? chunkAtCell.GetBottomHeightFast(num6, num7 + 1) : m_terrain.GetBottomHeight(j, k + 1));
				int x5 = MathUtils.Min(MathUtils.Min(x, x2, x3, x4), num9 - 1);
				int x6 = num8 + 2;
				x5 = MathUtils.Max(x5, 0);
				x6 = MathUtils.Min(x6, 256);
				int num10 = MathUtils.Max((x5 - 1) / 16, 0);
				int num11 = MathUtils.Min((x6 + 1) / 16, 15);
				int num12 = 1;
				num12 += Terrain.ExtractTemperature(shaftValueFast);
				num12 *= 31;
				num12 += Terrain.ExtractHumidity(shaftValueFast);
				num12 *= 31;
				for (int l = num10; l <= num11; l++)
				{
					int num13 = num12;
					int num14 = MathUtils.Max(l * 16 - 1, x5);
					int num15 = MathUtils.Min(l * 16 + 16 + 1, x6);
					int num16 = TerrainChunk.CalculateCellIndex(num6, num14, num7);
					int num17 = num16 + num15 - num14;
					while (num16 < num17)
					{
						num13 += chunkAtCell.GetCellValueFast(num16++);
						num13 *= 31;
					}
					num13 += num14;
					num13 *= 31;
					geometry.Slices[l].ContentsHash += num13;
				}
			}
		}
		double realTime2 = Time.RealTime;
		m_statistics.HashCount++;
		m_statistics.HashTime += realTime2 - realTime;
	}

	private void NotifyBlockBehaviors(TerrainChunk chunk)
	{
		ChunkInitialized?.Invoke(chunk);
		foreach (SubsystemBlockBehavior blockBehavior in m_subsystemBlockBehaviors.BlockBehaviors)
		{
			blockBehavior.OnChunkInitialized(chunk);
		}
		bool isLoaded = chunk.IsLoaded;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int x = i + chunk.Origin.X;
				int z = j + chunk.Origin.Y;
				int num = TerrainChunk.CalculateCellIndex(i, 0, j);
				int num2 = 0;
				while (num2 < 255)
				{
					int cellValueFast = chunk.GetCellValueFast(num);
					int num3 = Terrain.ExtractContents(cellValueFast);
					if (num3 != 0)
					{
						SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num3);
						for (int k = 0; k < blockBehaviors.Length; k++)
						{
							blockBehaviors[k].OnBlockGenerated(cellValueFast, x, num2, z, isLoaded);
						}
					}
					num2++;
					num++;
				}
			}
		}
	}

	private void UnpauseUpdateThread()
	{
		lock (m_unpauseLock)
		{
			m_unpauseUpdateThread = true;
			m_pauseEvent.Set();
		}
	}

	private void SettingsManager_SettingChanged(string name)
	{
		if (name == "Brightness")
		{
			DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: true);
		}
	}
}
