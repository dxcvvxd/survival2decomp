using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemPickables : Subsystem, IDrawable, IUpdateable
{
	private SubsystemAudio m_subsystemAudio;

	private SubsystemPlayers m_subsystemPlayers;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemSky m_subsystemSky;

	private SubsystemTime m_subsystemTime;

	private SubsystemGameInfo m_subsystemGameInfo;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemExplosions m_subsystemExplosions;

	private SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	private SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	private SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

	private List<ComponentPlayer> m_tmpPlayers = new List<ComponentPlayer>();

	private List<Pickable> m_pickables = new List<Pickable>();

	private List<Pickable> m_pickablesToRemove = new List<Pickable>();

	private PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

	private Random m_random = new Random();

	private DrawBlockEnvironmentData m_drawBlockEnvironmentData = new DrawBlockEnvironmentData();

	private static int[] m_drawOrders = new int[1] { 10 };

	public ReadOnlyList<Pickable> Pickables => new ReadOnlyList<Pickable>(m_pickables);

	public int[] DrawOrders => m_drawOrders;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public event Action<Pickable> PickableAdded;

	public event Action<Pickable> PickableRemoved;

	public Pickable AddPickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix)
	{
		Pickable pickable = new Pickable();
		pickable.Value = value;
		pickable.Count = count;
		pickable.Position = position;
		pickable.StuckMatrix = stuckMatrix;
		pickable.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
		if (velocity.HasValue)
		{
			pickable.Velocity = velocity.Value;
		}
		else if (Terrain.ExtractContents(value) == 248)
		{
			Vector2 vector = m_random.Vector2(1.5f, 2f);
			pickable.Velocity = new Vector3(vector.X, 3f, vector.Y);
		}
		else
		{
			pickable.Velocity = new Vector3(m_random.Float(-0.5f, 0.5f), m_random.Float(1f, 1.2f), m_random.Float(-0.5f, 0.5f));
		}
		m_pickables.Add(pickable);
		if (PickableAdded != null)
		{
			PickableAdded(pickable);
		}
		return pickable;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
		Matrix matrix = Matrix.CreateRotationY((float)MathUtils.Remainder(totalElapsedGameTime, 6.2831854820251465));
		float num = MathUtils.Min(m_subsystemSky.VisibilityRange, 48f);
		foreach (Pickable pickable in m_pickables)
		{
			Vector3 position = pickable.Position;
			Vector3 v = position - camera.ViewPosition;
			float num2 = Vector3.Dot(camera.ViewDirection, v);
			if (num2 < -0.5f || num2 > num)
			{
				continue;
			}
			float num3 = v.Length();
			if (!(num3 > num))
			{
				int num4 = Terrain.ExtractContents(pickable.Value);
				Block block = BlocksManager.Blocks[num4];
				float num5 = (float)(totalElapsedGameTime - pickable.CreationTime);
				if (!pickable.StuckMatrix.HasValue)
				{
					position.Y += 0.25f * MathUtils.Saturate(3f * num5);
				}
				int x = Terrain.ToCell(position.X);
				int num6 = Terrain.ToCell(position.Y);
				int z = Terrain.ToCell(position.Z);
				TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
				if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidVertices1 && num6 >= 0 && num6 < 255)
				{
					m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
					m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num6);
					float f = MathUtils.Max(position.Y - (float)num6 - 0.75f, 0f) / 0.25f;
					int light = (int)MathUtils.Lerp(m_subsystemTerrain.Terrain.GetCellLightFast(x, num6, z), m_subsystemTerrain.Terrain.GetCellLightFast(x, num6 + 1, z), f);
					pickable.Light = light;
				}
				m_drawBlockEnvironmentData.Light = pickable.Light;
				m_drawBlockEnvironmentData.BillboardDirection = pickable.Position - camera.ViewPosition;
				m_drawBlockEnvironmentData.InWorldMatrix.Translation = position;
				float num7 = 1f - m_subsystemSky.CalculateFog(camera.ViewPosition, pickable.Position);
				num7 *= MathUtils.Saturate(0.25f * (num - num3));
				Color color = Color.MultiplyNotSaturated(Color.White, num7);
				if (pickable.StuckMatrix.HasValue)
				{
					Matrix matrix2 = pickable.StuckMatrix.Value;
					block.DrawBlock(m_primitivesRenderer, pickable.Value, color, 0.3f, ref matrix2, m_drawBlockEnvironmentData);
				}
				else
				{
					matrix.Translation = position + new Vector3(0f, 0.03f * (MathUtils.Sin(3f * num5) + 1f), 0f);
					block.DrawBlock(m_primitivesRenderer, pickable.Value, color, 0.3f, ref matrix, m_drawBlockEnvironmentData);
				}
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	public void Update(float dt)
	{
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		float num = MathUtils.Pow(0.5f, dt);
		float num2 = MathUtils.Pow(0.001f, dt);
		m_tmpPlayers.Clear();
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			if (componentPlayer.ComponentHealth.Health > 0f)
			{
				m_tmpPlayers.Add(componentPlayer);
			}
		}
		foreach (Pickable pickable in m_pickables)
		{
			if (pickable.ToRemove)
			{
				m_pickablesToRemove.Add(pickable);
				continue;
			}
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
			int num3 = m_pickables.Count - m_pickablesToRemove.Count;
			string category = block.GetCategory(pickable.Value);
			float num4 = ((category == "Terrain") ? ((float)((num3 > 80) ? 60 : 120)) : ((category == "Plants" && block.GetNutritionalValue(pickable.Value) == 0f) ? ((float)((num3 > 80) ? 60 : 120)) : ((!(block is EggBlock)) ? ((float)((num3 > 80) ? 120 : 480)) : 240f)));
			double num5 = totalElapsedGameTime - pickable.CreationTime;
			if (num5 > (double)num4)
			{
				pickable.ToRemove = true;
				continue;
			}
			TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Z));
			if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidContents4)
			{
				continue;
			}
			Vector3 position = pickable.Position;
			Vector3 vector = position + pickable.Velocity * dt;
			if (!pickable.FlyToPosition.HasValue && num5 > 0.5)
			{
				foreach (ComponentPlayer tmpPlayer in m_tmpPlayers)
				{
					ComponentBody componentBody = tmpPlayer.ComponentBody;
					Vector3 vector2 = componentBody.Position + new Vector3(0f, 0.75f, 0f);
					float num6 = (vector2 - pickable.Position).LengthSquared();
					if (!(num6 < 3.0625f))
					{
						continue;
					}
					bool flag = Terrain.ExtractContents(pickable.Value) == 248;
					IInventory inventory = tmpPlayer.ComponentMiner.Inventory;
					if (!flag && ComponentInventoryBase.FindAcquireSlotForItem(inventory, pickable.Value) < 0)
					{
						continue;
					}
					if (num6 < 1f)
					{
						if (flag)
						{
							tmpPlayer.ComponentLevel.AddExperience(pickable.Count, playSound: true);
							pickable.ToRemove = true;
							continue;
						}
						pickable.Count = ComponentInventoryBase.AcquireItems(inventory, pickable.Value, pickable.Count);
						if (pickable.Count == 0)
						{
							pickable.ToRemove = true;
							m_subsystemAudio.PlaySound("Audio/PickableCollected", 0.7f, -0.4f, pickable.Position, 2f, autoDelay: false);
						}
					}
					else if (!pickable.StuckMatrix.HasValue)
					{
						pickable.FlyToPosition = vector2 + 0.1f * MathUtils.Sqrt(num6) * componentBody.Velocity;
					}
				}
			}
			if (pickable.FlyToPosition.HasValue)
			{
				Vector3 vector3 = pickable.FlyToPosition.Value - pickable.Position;
				float num7 = vector3.LengthSquared();
				if (num7 >= 0.25f)
				{
					pickable.Velocity = 6f * vector3 / MathUtils.Sqrt(num7);
				}
				else
				{
					pickable.FlyToPosition = null;
				}
			}
			else
			{
				Vector2? vector4 = m_subsystemFluidBlockBehavior.CalculateFlowSpeed(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y + 0.1f), Terrain.ToCell(pickable.Position.Z), out var surfaceBlock, out var surfaceHeight);
				if (!pickable.StuckMatrix.HasValue)
				{
					TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position, vector, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable);
					if (terrainRaycastResult.HasValue)
					{
						int contents = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(terrainRaycastResult.Value.CellFace.X, terrainRaycastResult.Value.CellFace.Y, terrainRaycastResult.Value.CellFace.Z));
						SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(contents);
						for (int num8 = 0; num8 < blockBehaviors.Length; num8++)
						{
							blockBehaviors[num8].OnHitByProjectile(terrainRaycastResult.Value.CellFace, pickable);
						}
						if (m_subsystemTerrain.Raycast(position, position, useInteractionBoxes: false, skipAirBlocks: true, (int value2, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value2)].IsCollidable).HasValue)
						{
							int num9 = Terrain.ToCell(position.X);
							int num10 = Terrain.ToCell(position.Y);
							int num11 = Terrain.ToCell(position.Z);
							int num12 = 0;
							int num13 = 0;
							int num14 = 0;
							int? num15 = null;
							for (int num16 = -3; num16 <= 3; num16++)
							{
								for (int num17 = -3; num17 <= 3; num17++)
								{
									for (int num18 = -3; num18 <= 3; num18++)
									{
										if (!BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num16 + num9, num17 + num10, num18 + num11)].IsCollidable)
										{
											int num19 = num16 * num16 + num17 * num17 + num18 * num18;
											if (!num15.HasValue || num19 < num15.Value)
											{
												num12 = num16 + num9;
												num13 = num17 + num10;
												num14 = num18 + num11;
												num15 = num19;
											}
										}
									}
								}
							}
							if (num15.HasValue)
							{
								pickable.FlyToPosition = new Vector3(num12, num13, num14) + new Vector3(0.5f);
							}
							else
							{
								pickable.ToRemove = true;
							}
						}
						else
						{
							Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
							bool flag2 = vector4.HasValue && vector4.Value != Vector2.Zero;
							if (plane.Normal.X != 0f)
							{
								float num20 = ((flag2 || MathUtils.Sqrt(MathUtils.Sqr(pickable.Velocity.Y) + MathUtils.Sqr(pickable.Velocity.Z)) > 10f) ? 0.95f : 0.25f);
								pickable.Velocity *= new Vector3(0f - num20, num20, num20);
							}
							if (plane.Normal.Y != 0f)
							{
								float num21 = ((flag2 || MathUtils.Sqrt(MathUtils.Sqr(pickable.Velocity.X) + MathUtils.Sqr(pickable.Velocity.Z)) > 10f) ? 0.95f : 0.25f);
								pickable.Velocity *= new Vector3(num21, 0f - num21, num21);
								if (flag2)
								{
									pickable.Velocity.Y += 0.1f * plane.Normal.Y;
								}
							}
							if (plane.Normal.Z != 0f)
							{
								float num22 = ((flag2 || MathUtils.Sqrt(MathUtils.Sqr(pickable.Velocity.X) + MathUtils.Sqr(pickable.Velocity.Y)) > 10f) ? 0.95f : 0.25f);
								pickable.Velocity *= new Vector3(num22, num22, 0f - num22);
							}
							vector = position;
						}
					}
				}
				else
				{
					Vector3 vector5 = pickable.StuckMatrix.Value.Translation + pickable.StuckMatrix.Value.Up * block.ProjectileTipOffset;
					if (!m_subsystemTerrain.Raycast(vector5, vector5, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable).HasValue)
					{
						pickable.Position = pickable.StuckMatrix.Value.Translation;
						pickable.Velocity = Vector3.Zero;
						pickable.StuckMatrix = null;
					}
				}
				if (surfaceBlock is WaterBlock && !pickable.SplashGenerated)
				{
					m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, pickable.Position, large: false));
					m_subsystemAudio.PlayRandomSound("Audio/Splashes", 1f, m_random.Float(-0.2f, 0.2f), pickable.Position, 6f, autoDelay: true);
					pickable.SplashGenerated = true;
				}
				else if (surfaceBlock is MagmaBlock && !pickable.SplashGenerated)
				{
					m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemTerrain, pickable.Position, large: false));
					m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), pickable.Position, 3f, autoDelay: true);
					pickable.ToRemove = true;
					pickable.SplashGenerated = true;
					m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y), Terrain.ToCell(pickable.Position.Z), pickable.Value);
				}
				else if (surfaceBlock == null)
				{
					pickable.SplashGenerated = false;
				}
				if (m_subsystemTime.PeriodicGameTimeEvent(1.0, (double)(pickable.GetHashCode() % 100) / 100.0) && (m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y + 0.1f), Terrain.ToCell(pickable.Position.Z)) == 104 || m_subsystemFireBlockBehavior.IsCellOnFire(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y + 0.1f), Terrain.ToCell(pickable.Position.Z))))
				{
					m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), pickable.Position, 3f, autoDelay: true);
					pickable.ToRemove = true;
					m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y), Terrain.ToCell(pickable.Position.Z), pickable.Value);
				}
				if (!pickable.StuckMatrix.HasValue)
				{
					if (vector4.HasValue && surfaceHeight.HasValue)
					{
						float num23 = surfaceHeight.Value - pickable.Position.Y;
						float num24 = MathUtils.Saturate(3f * num23);
						pickable.Velocity.X += 4f * dt * (vector4.Value.X - pickable.Velocity.X);
						pickable.Velocity.Y -= 10f * dt;
						pickable.Velocity.Y += 10f * (1f / block.Density * num24) * dt;
						pickable.Velocity.Z += 4f * dt * (vector4.Value.Y - pickable.Velocity.Z);
						pickable.Velocity.Y *= num2;
					}
					else
					{
						pickable.Velocity.Y -= 10f * dt;
						pickable.Velocity *= num;
					}
				}
			}
			pickable.Position = vector;
		}
		foreach (Pickable item in m_pickablesToRemove)
		{
			m_pickables.Remove(item);
			if (PickableRemoved != null)
			{
				PickableRemoved(item);
			}
		}
		m_pickablesToRemove.Clear();
	}

	protected override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemPlayers = base.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemExplosions = base.Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_subsystemFireBlockBehavior = base.Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemFluidBlockBehavior = base.Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
		foreach (ValuesDictionary item in valuesDictionary.GetValue<ValuesDictionary>("Pickables").Values.Where((object v) => v is ValuesDictionary))
		{
			Pickable pickable = new Pickable();
			pickable.Value = item.GetValue<int>("Value");
			pickable.Count = item.GetValue<int>("Count");
			pickable.Position = item.GetValue<Vector3>("Position");
			pickable.Velocity = item.GetValue<Vector3>("Velocity");
			pickable.CreationTime = item.GetValue("CreationTime", 0.0);
			if (item.ContainsKey("StuckMatrix"))
			{
				pickable.StuckMatrix = item.GetValue<Matrix>("StuckMatrix");
			}
			m_pickables.Add(pickable);
		}
	}

	protected override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Pickables", valuesDictionary2);
		int num = 0;
		foreach (Pickable pickable in m_pickables)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary2.SetValue(num.ToString(), valuesDictionary3);
			valuesDictionary3.SetValue("Value", pickable.Value);
			valuesDictionary3.SetValue("Count", pickable.Count);
			valuesDictionary3.SetValue("Position", pickable.Position);
			valuesDictionary3.SetValue("Velocity", pickable.Velocity);
			valuesDictionary3.SetValue("CreationTime", pickable.CreationTime);
			if (pickable.StuckMatrix.HasValue)
			{
				valuesDictionary3.SetValue("StuckMatrix", pickable.StuckMatrix.Value);
			}
			num++;
		}
	}
}
