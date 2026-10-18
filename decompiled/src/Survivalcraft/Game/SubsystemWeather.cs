using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Audio;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemWeather : Subsystem, IDrawable, IUpdateable
{
	private SubsystemGameInfo m_subsystemGameInfo;

	private SubsystemBlocksScanner m_subsystemBlocksScanner;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemSeasons m_subsystemSeasons;

	private Random m_random = new Random();

	private Dictionary<GameWidget, Dictionary<Point2, PrecipitationShaftParticleSystem>> m_activeShafts = new Dictionary<GameWidget, Dictionary<Point2, PrecipitationShaftParticleSystem>>();

	private List<PrecipitationShaftParticleSystem> m_toRemove = new List<PrecipitationShaftParticleSystem>();

	private Dictionary<GameWidget, Vector2?> m_lastShaftsUpdatePositions = new Dictionary<GameWidget, Vector2?>();

	private float m_targetRainSoundVolume;

	private double m_precipitationStartTime;

	private double m_precipitationEndTime;

	private float m_precipitationRampTime;

	private float m_lightningIntensity;

	private double m_fogStartTime;

	private double m_fogEndTime;

	private float m_fogRampTime;

	private const int m_rainSoundRadius = 7;

	private float m_rainVolumeFactor;

	private Sound m_rainSound;

	private int[] m_shuffledOrder;

	private static int[] m_drawOrders = new int[1] { 50 };

	public SubsystemTerrain SubsystemTerrain { get; private set; }

	public SubsystemSky SubsystemSky { get; private set; }

	public SubsystemTime SubsystemTime { get; private set; }

	public RainSplashParticleSystem RainSplashParticleSystem { get; private set; }

	public SnowSplashParticleSystem SnowSplashParticleSystem { get; private set; }

	public Color RainColor { get; private set; }

	public Color SnowColor { get; private set; }

	public float PrecipitationIntensity { get; private set; }

	public int FogSeed { get; private set; }

	public float FogProgress { get; private set; }

	public float FogIntensity { get; private set; }

	public bool IsPrecipitationStarted
	{
		get
		{
			if (m_subsystemGameInfo.TotalElapsedGameTime >= m_precipitationStartTime)
			{
				return m_subsystemGameInfo.TotalElapsedGameTime < m_precipitationEndTime - (double)m_precipitationRampTime;
			}
			return false;
		}
	}

	public bool IsFogStarted
	{
		get
		{
			if (m_subsystemGameInfo.TotalElapsedGameTime >= m_fogStartTime)
			{
				return m_subsystemGameInfo.TotalElapsedGameTime < m_fogEndTime - (double)m_fogRampTime;
			}
			return false;
		}
	}

	public int[] DrawOrders => m_drawOrders;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public PrecipitationShaftInfo GetPrecipitationShaftInfo(int x, int z)
	{
		int shaftValue = SubsystemTerrain.Terrain.GetShaftValue(x, z);
		int seasonalTemperature = SubsystemTerrain.Terrain.GetSeasonalTemperature(shaftValue);
		int num = Terrain.ExtractTopHeight(shaftValue);
		if (IsPlaceFrozen(seasonalTemperature, num))
		{
			return new PrecipitationShaftInfo
			{
				Intensity = PrecipitationIntensity,
				Type = PrecipitationType.Snow,
				YLimit = num + 1
			};
		}
		int seasonalHumidity = SubsystemTerrain.Terrain.GetSeasonalHumidity(shaftValue);
		if (seasonalTemperature <= 8 || seasonalHumidity >= 8)
		{
			return new PrecipitationShaftInfo
			{
				Intensity = PrecipitationIntensity,
				Type = PrecipitationType.Rain,
				YLimit = num + 1
			};
		}
		return new PrecipitationShaftInfo
		{
			Intensity = 0f,
			Type = PrecipitationType.Rain,
			YLimit = num + 1
		};
	}

	public void ManualLightingStrike(Vector3 position, Vector3 direction)
	{
		int num = Terrain.ToCell(position.X + direction.X * 32f);
		int num2 = Terrain.ToCell(position.Z + direction.Z * 32f);
		Vector3? vector = null;
		for (int i = 0; i < 300; i++)
		{
			int num3 = m_random.Int(-8, 8);
			int num4 = m_random.Int(-8, 8);
			int num5 = num + num3;
			int num6 = num2 + num4;
			int num7 = SubsystemTerrain.Terrain.CalculateTopmostCellHeight(num5, num6);
			if (!vector.HasValue || (float)num7 > vector.Value.Y)
			{
				vector = new Vector3(num5, num7, num6);
			}
		}
		if (vector.HasValue)
		{
			SubsystemSky.MakeLightningStrike(vector.Value, manual: true);
		}
	}

	public void ManualPrecipitationStart()
	{
		m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
		m_precipitationEndTime = double.PositiveInfinity;
		m_precipitationRampTime = 1f;
	}

	public void ManualPrecipitationEnd()
	{
		m_precipitationRampTime = 1f;
		m_precipitationEndTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)m_precipitationRampTime;
	}

	public void ManualFogStart()
	{
		m_fogStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
		m_fogEndTime = double.PositiveInfinity;
		m_fogRampTime = 3f;
	}

	public void ManualFogEnd()
	{
		m_fogRampTime = 3f;
		m_fogEndTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)m_fogRampTime;
	}

	public static int GetTemperatureAdjustmentAtHeight(int y)
	{
		return (int)MathUtils.Round((y > 64) ? (-0.0008f * (float)MathUtils.Sqr(y - 64)) : (0.1f * (float)(64 - y)));
	}

	public static bool IsPlaceFrozen(int temperature, int y)
	{
		return temperature + GetTemperatureAdjustmentAtHeight(y) <= 0;
	}

	public static bool ShaftHasSnowOnIce(int x, int z)
	{
		return MathUtils.Hash((uint)((x & 0xFFFF) | (z << 16))) > 429496729;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		int num = ((SettingsManager.VisibilityRange > 128) ? 9 : ((SettingsManager.VisibilityRange <= 64) ? 7 : 8));
		int num2 = num * num;
		Dictionary<Point2, PrecipitationShaftParticleSystem> activeShafts = GetActiveShafts(camera.GameWidget);
		byte b = (byte)(255f * MathUtils.Lerp(0.15f, 1f, SubsystemSky.SkyLightIntensity));
		RainColor = new Color(b, b, b);
		byte b2 = (byte)(255f * MathUtils.Lerp(0.15f, 1f, SubsystemSky.SkyLightIntensity));
		SnowColor = new Color(b2, b2, b2);
		Vector2 vector = new Vector2(camera.ViewPosition.X, camera.ViewPosition.Z);
		Point2 point = Terrain.ToCell(vector);
		Vector2? value = null;
		m_lastShaftsUpdatePositions.TryGetValue(camera.GameWidget, out value);
		if (value.HasValue && !(Vector2.DistanceSquared(value.Value, vector) > 1f))
		{
			return;
		}
		m_lastShaftsUpdatePositions[camera.GameWidget] = vector;
		m_toRemove.Clear();
		foreach (PrecipitationShaftParticleSystem value2 in activeShafts.Values)
		{
			if (MathUtils.Sqr((float)value2.Point.X + 0.5f - vector.X) + MathUtils.Sqr((float)value2.Point.Y + 0.5f - vector.Y) > (float)num2 + 1f)
			{
				m_toRemove.Add(value2);
			}
		}
		foreach (PrecipitationShaftParticleSystem item in m_toRemove)
		{
			if (m_subsystemParticles.ContainsParticleSystem(item))
			{
				m_subsystemParticles.RemoveParticleSystem(item);
			}
			activeShafts.Remove(item.Point);
		}
		for (int i = point.X - num; i <= point.X + num; i++)
		{
			for (int j = point.Y - num; j <= point.Y + num; j++)
			{
				if (MathUtils.Sqr((float)i + 0.5f - vector.X) + MathUtils.Sqr((float)j + 0.5f - vector.Y) <= (float)num2)
				{
					Point2 point2 = new Point2(i, j);
					if (!activeShafts.ContainsKey(point2))
					{
						PrecipitationShaftParticleSystem precipitationShaftParticleSystem = new PrecipitationShaftParticleSystem(camera.GameWidget, this, m_random, point2);
						m_subsystemParticles.AddParticleSystem(precipitationShaftParticleSystem);
						activeShafts.Add(point2, precipitationShaftParticleSystem);
					}
				}
			}
		}
	}

	public void Update(float dt)
	{
		UpdatePrecipitation(dt);
		UpdateLightning(dt);
		UpdateFog(dt);
	}

	protected override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		SubsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemBlocksScanner = base.Project.FindSubsystem<SubsystemBlocksScanner>(throwOnError: true);
		SubsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		SubsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemSeasons = base.Project.FindSubsystem<SubsystemSeasons>(throwOnError: true);
		m_precipitationStartTime = valuesDictionary.GetValue<double>("WeatherStartTime");
		m_precipitationEndTime = valuesDictionary.GetValue<double>("WeatherEndTime");
		m_precipitationRampTime = valuesDictionary.GetValue("WeatherRampTime", 25f);
		PrecipitationIntensity = valuesDictionary.GetValue("PrecipitationIntensity", 0f);
		m_lightningIntensity = valuesDictionary.GetValue<float>("LightningIntensity");
		m_fogStartTime = valuesDictionary.GetValue<double>("FogStartTime");
		m_fogEndTime = valuesDictionary.GetValue<double>("FogEndTime");
		m_fogRampTime = valuesDictionary.GetValue("FogRampTime", 25f);
		FogIntensity = valuesDictionary.GetValue("FogIntensity", 0f);
		FogProgress = valuesDictionary.GetValue("FogProgress", 0f);
		m_rainSound = m_subsystemAudio.CreateSound("Audio/Rain");
		m_rainSound.IsLooped = true;
		m_rainSound.Volume = 0f;
		RainSplashParticleSystem = new RainSplashParticleSystem();
		m_subsystemParticles.AddParticleSystem(RainSplashParticleSystem);
		SnowSplashParticleSystem = new SnowSplashParticleSystem();
		m_subsystemParticles.AddParticleSystem(SnowSplashParticleSystem);
		m_rainVolumeFactor = 0f;
		for (int i = -7; i <= 7; i++)
		{
			for (int j = -7; j <= 7; j++)
			{
				float distance = MathUtils.Sqrt(i * i + j * j);
				m_rainVolumeFactor += m_subsystemAudio.CalculateVolume(distance, 1f);
			}
		}
		m_subsystemBlocksScanner.ScanningChunkCompleted += delegate(TerrainChunk chunk)
		{
			if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living)
			{
				FreezeThawAndDepositSnow(chunk, 0.66f, 0.66f, forceDepositSnow: false);
			}
		};
		SubsystemTerrain.TerrainUpdater.ChunkInitialized += delegate(TerrainChunk chunk)
		{
			if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living)
			{
				FreezeThawAndDepositSnow(chunk, 1f, 1f, m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled);
				FreezeThawAndDepositSnow(chunk, 0.66f, 0.66f, m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled);
			}
		};
	}

	protected override void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("WeatherStartTime", m_precipitationStartTime);
		valuesDictionary.SetValue("WeatherEndTime", m_precipitationEndTime);
		valuesDictionary.SetValue("WeatherRampTime", m_precipitationRampTime);
		valuesDictionary.SetValue("WeatherIntensity", PrecipitationIntensity);
		valuesDictionary.SetValue("LightningIntensity", m_lightningIntensity);
		valuesDictionary.SetValue("FogStartTime", m_fogStartTime);
		valuesDictionary.SetValue("FogEndTime", m_fogEndTime);
		valuesDictionary.SetValue("FogRampTime", m_fogRampTime);
		valuesDictionary.SetValue("FogIntensity", FogIntensity);
		valuesDictionary.SetValue("FogProgress", FogProgress);
	}

	private void UpdatePrecipitation(float dt)
	{
		if (m_subsystemGameInfo.TotalElapsedGameTime > m_precipitationEndTime)
		{
			float num = 0f;
			float num2 = 0f;
			float probability = 0f;
			switch (m_subsystemSeasons.Season)
			{
			case Season.Summer:
				num = 1f;
				num2 = 1f;
				probability = 0.5f;
				break;
			case Season.Autumn:
				num = 1.5f;
				num2 = 1f;
				probability = 0.5f;
				break;
			case Season.Winter:
				num = 1f;
				num2 = 0f;
				probability = 0f;
				break;
			case Season.Spring:
				num = 1.5f;
				num2 = 2f;
				probability = 1f;
				break;
			}
			if (m_precipitationEndTime == 0.0)
			{
				if (m_subsystemGameInfo.WorldSettings.StartingPositionMode == StartingPositionMode.Hard)
				{
					m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(0f, 2f));
					m_lightningIntensity = m_random.Float(0.66f, 1f) * num2;
				}
				else
				{
					m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(3f, 6f));
					m_lightningIntensity = m_random.Float(0.33f, 0.66f) * num2;
				}
			}
			else
			{
				m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(5f, 45f) / num);
				m_lightningIntensity = (m_random.Bool(probability) ? MathUtils.Saturate(m_random.Float(0.33f, 1f) * num2) : 0f);
			}
			m_precipitationEndTime = m_precipitationStartTime + (double)(60f * m_random.Float(3f, 6f));
			m_precipitationRampTime = m_random.Float(10f, 30f);
		}
		if (m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled)
		{
			if (IsPrecipitationStarted)
			{
				PrecipitationIntensity = MathUtils.Saturate(PrecipitationIntensity + dt / m_precipitationRampTime);
			}
			else
			{
				PrecipitationIntensity = MathUtils.Saturate(PrecipitationIntensity - dt / m_precipitationRampTime);
			}
		}
		else
		{
			PrecipitationIntensity = 0f;
		}
		if (Time.PeriodicEvent(0.33000001311302185, 0.0))
		{
			float num3 = 0f;
			if (PrecipitationIntensity > 0f)
			{
				float num4 = 0f;
				Vector3 vector = default(Vector3);
				foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions)
				{
					int num5 = Terrain.ToCell(listenerPosition.X) - 7;
					int num6 = Terrain.ToCell(listenerPosition.Z) - 7;
					int num7 = Terrain.ToCell(listenerPosition.X) + 7;
					int num8 = Terrain.ToCell(listenerPosition.Z) + 7;
					for (int i = num5; i <= num7; i++)
					{
						for (int j = num6; j <= num8; j++)
						{
							PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(i, j);
							if (precipitationShaftInfo.Type == PrecipitationType.Rain && precipitationShaftInfo.Intensity > 0f)
							{
								vector.X = (float)i + 0.5f;
								vector.Y = MathUtils.Max(precipitationShaftInfo.YLimit, listenerPosition.Y);
								vector.Z = (float)j + 0.5f;
								float num9 = vector.X - listenerPosition.X;
								float num10 = 8f * (vector.Y - listenerPosition.Y);
								float num11 = vector.Z - listenerPosition.Z;
								float distance = MathUtils.Sqrt(num9 * num9 + num10 * num10 + num11 * num11);
								num4 += m_subsystemAudio.CalculateVolume(distance, 1f) * precipitationShaftInfo.Intensity;
							}
						}
					}
				}
				num3 = MathUtils.Max(num3, num4);
			}
			m_targetRainSoundVolume = MathUtils.Saturate(2f * num3 / m_rainVolumeFactor);
		}
		m_rainSound.Volume = MathUtils.Saturate(MathUtils.Lerp(m_rainSound.Volume, SettingsManager.SoundsVolume * m_targetRainSoundVolume, 5f * dt));
		if (m_rainSound.Volume > AudioManager.MinAudibleVolume)
		{
			m_rainSound.Play();
		}
		else
		{
			m_rainSound.Pause();
		}
	}

	private void UpdateLightning(float dt)
	{
		if (PrecipitationIntensity != 1f || !SubsystemTime.PeriodicGameTimeEvent(1.0, 0.0))
		{
			return;
		}
		TerrainChunk[] allocatedChunks = SubsystemTerrain.Terrain.AllocatedChunks;
		for (int i = 0; i < allocatedChunks.Length; i++)
		{
			TerrainChunk terrainChunk = allocatedChunks[m_random.Int(0, allocatedChunks.Length - 1)];
			if (terrainChunk.State < TerrainChunkState.InvalidVertices1 || !m_random.Bool(m_lightningIntensity * 0.0002f))
			{
				continue;
			}
			int num = terrainChunk.Origin.X + m_random.Int(0, 15);
			int num2 = terrainChunk.Origin.Y + m_random.Int(0, 15);
			Vector3? vector = null;
			for (int j = num - 8; j < num + 8; j++)
			{
				for (int k = num2 - 8; k < num2 + 8; k++)
				{
					int topHeight = SubsystemTerrain.Terrain.GetTopHeight(j, k);
					if (!vector.HasValue || (float)topHeight > vector.Value.Y)
					{
						vector = new Vector3(j, topHeight, k);
					}
				}
			}
			if (vector.HasValue)
			{
				SubsystemSky.MakeLightningStrike(vector.Value, manual: false);
				break;
			}
		}
	}

	private void UpdateFog(float dt)
	{
		if (m_subsystemGameInfo.TotalElapsedGameTime > m_fogEndTime)
		{
			float num = ((m_subsystemSeasons.Season == Season.Autumn || m_subsystemSeasons.Season == Season.Winter) ? 1.75f : 1f);
			if (m_fogEndTime == 0.0 && m_subsystemGameInfo.WorldSettings.StartingPositionMode == StartingPositionMode.Hard)
			{
				m_fogStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(1f, 10f) / num);
			}
			else
			{
				m_fogStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(10f, 40f) / num);
			}
			m_fogEndTime = m_fogStartTime + (double)(60f * m_random.Float(4f, 7f) * num);
			m_fogRampTime = m_random.Float(20f, 40f);
			FogProgress = 0f;
		}
		FogSeed = MathUtils.Hash((int)MathUtils.Remainder(m_fogStartTime, 1000000.0));
		if (m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled)
		{
			if (IsFogStarted)
			{
				FogIntensity = MathUtils.Saturate(FogIntensity + dt / m_fogRampTime);
				FogProgress += dt / (float)(m_fogEndTime - m_fogStartTime);
			}
			else
			{
				FogIntensity = MathUtils.Saturate(FogIntensity - dt / m_fogRampTime);
			}
		}
		else
		{
			FogIntensity = 0f;
		}
	}

	private Dictionary<Point2, PrecipitationShaftParticleSystem> GetActiveShafts(GameWidget gameWidget)
	{
		if (!m_activeShafts.TryGetValue(gameWidget, out var value))
		{
			value = new Dictionary<Point2, PrecipitationShaftParticleSystem>();
			m_activeShafts.Add(gameWidget, value);
		}
		return value;
	}

	private void FreezeThawAndDepositSnow(TerrainChunk chunk, float freezeProbability, float thawProbability, bool forceDepositSnow)
	{
		if (m_shuffledOrder == null)
		{
			m_shuffledOrder = Enumerable.Range(0, 256).ToArray();
		}
		m_shuffledOrder.RandomShuffle((int i) => m_random.Int(i));
		Terrain terrain = SubsystemTerrain.Terrain;
		for (int num = 0; num < m_shuffledOrder.Length; num++)
		{
			int num2 = m_shuffledOrder[num] & 0xF;
			int num3 = m_shuffledOrder[num] >> 4;
			int num4 = chunk.GetTopHeightFast(num2, num3);
			int cellValueFast = chunk.GetCellValueFast(num2, num4, num3);
			int num5 = Terrain.ExtractContents(cellValueFast);
			int num6 = chunk.Origin.X + num2;
			int num7 = num4;
			int num8 = chunk.Origin.Y + num3;
			PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(num6, num8);
			if (precipitationShaftInfo.Type == PrecipitationType.Snow)
			{
				if (!m_random.Bool(freezeProbability))
				{
					continue;
				}
				if (num5 == 18 && SubsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(num6, num8) > -35f)
				{
					int cellContents = terrain.GetCellContents(num6 + 1, num7, num8);
					int cellContents2 = terrain.GetCellContents(num6 - 1, num7, num8);
					int cellContents3 = terrain.GetCellContents(num6, num7, num8 - 1);
					int cellContents4 = terrain.GetCellContents(num6, num7, num8 + 1);
					bool num9 = BlocksManager.FluidBlocks[cellContents] == null && cellContents != 0;
					bool flag = BlocksManager.FluidBlocks[cellContents2] == null && cellContents2 != 0;
					bool flag2 = BlocksManager.FluidBlocks[cellContents3] == null && cellContents3 != 0;
					bool flag3 = BlocksManager.FluidBlocks[cellContents4] == null && cellContents4 != 0;
					if (num9 | flag | flag2 | flag3)
					{
						SubsystemTerrain.ChangeCell(num6, num7, num8, Terrain.MakeBlockValue(62));
					}
				}
				else
				{
					if ((!forceDepositSnow && !(precipitationShaftInfo.Intensity > 0.5f)) || num7 + 1 >= 255)
					{
						continue;
					}
					if (SubsystemSnowBlockBehavior.CanSupportSnow(cellValueFast))
					{
						if (num5 != 62 || ShaftHasSnowOnIce(num6, num8))
						{
							SubsystemTerrain.ChangeCell(num6, num7 + 1, num8, Terrain.MakeBlockValue(61));
						}
					}
					else if (SubsystemSnowBlockBehavior.CanBeReplacedBySnow(cellValueFast))
					{
						SubsystemTerrain.ChangeCell(num6, num7, num8, Terrain.MakeBlockValue(61));
					}
				}
			}
			else
			{
				if (!m_random.Bool(thawProbability))
				{
					continue;
				}
				for (; num7 > 0; num4--, num7--, cellValueFast = chunk.GetCellValueFast(num2, num4, num3), num5 = Terrain.ExtractContents(cellValueFast))
				{
					switch (num5)
					{
					case 61:
						SubsystemTerrain.DestroyCell(0, num6, num7, num8, 0, noDrop: true, noParticleSystem: true);
						continue;
					case 62:
						SubsystemTerrain.DestroyCell(0, num6, num7, num8, 0, noDrop: false, noParticleSystem: true);
						continue;
					}
					break;
				}
			}
		}
	}
}
