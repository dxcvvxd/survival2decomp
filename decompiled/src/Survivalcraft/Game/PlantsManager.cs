using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game;

public static class PlantsManager
{
	private static List<TerrainBrush>[] m_treeBrushesByType;

	private static int[] m_treeTrunksByType;

	private static int[] m_treeLeavesByType;

	static PlantsManager()
	{
		m_treeBrushesByType = new List<TerrainBrush>[EnumUtils.GetEnumValues(typeof(TreeType)).Max() + 1];
		m_treeTrunksByType = new int[6] { 9, 10, 11, 11, 255, 262 };
		m_treeLeavesByType = new int[6]
		{
			Terrain.MakeBlockValue(12, 0, DeciduousLeavesBlock.SetSeason(0, Season.Spring)),
			Terrain.MakeBlockValue(13, 0, DeciduousLeavesBlock.SetSeason(0, Season.Spring)),
			14,
			225,
			Terrain.MakeBlockValue(256, 0, DeciduousLeavesBlock.SetSeason(0, Season.Spring)),
			Terrain.MakeBlockValue(263, 0, DeciduousLeavesBlock.SetSeason(0, Season.Spring))
		};
		Random random = new Random(33);
		m_treeBrushesByType[0] = new List<TerrainBrush>();
		for (int i = 0; i < 16; i++)
		{
			int[] array = new int[16]
			{
				5, 6, 7, 8, 9, 10, 11, 11, 12, 12,
				13, 13, 14, 15, 16, 18
			};
			int height = array[i];
			int branchesCount = (int)MathUtils.Lerp(10f, 22f, (float)i / 16f);
			TerrainBrush item = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.Oak), GetTreeLeavesValue(TreeType.Oak), height, branchesCount, 3, delegate(int y, int round)
			{
				float num6 = 0.4f;
				if ((float)y < 0.2f * (float)height)
				{
					num6 = 0f;
				}
				else if ((float)y >= 0.2f * (float)height && y <= height)
				{
					num6 *= 1.5f;
				}
				return num6;
			}, delegate(int y)
			{
				if ((float)y < (float)height * 0.3f || (float)y > (float)height * 0.9f)
				{
					return 0f;
				}
				float num6 = (((float)y < (float)height * 0.7f) ? (0.5f * (float)height) : (0.35f * (float)height));
				return random.Float(0.33f, 1f) * num6;
			});
			m_treeBrushesByType[0].Add(item);
		}
		m_treeBrushesByType[1] = new List<TerrainBrush>();
		for (int num = 0; num < 16; num++)
		{
			int[] array2 = new int[16]
			{
				4, 5, 6, 7, 7, 8, 8, 9, 9, 9,
				10, 10, 10, 11, 11, 12
			};
			int height2 = array2[num];
			int branchesCount2 = (int)MathUtils.Lerp(0f, 20f, (float)num / 16f);
			TerrainBrush item2 = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.Birch), GetTreeLeavesValue(TreeType.Birch), height2, branchesCount2, 3, delegate(int y, int round)
			{
				float num6 = 0.66f;
				if (y < height2 / 2 - 1)
				{
					num6 = 0f;
				}
				else if (y > height2 / 2 && y <= height2)
				{
					num6 *= 1.5f;
				}
				return num6;
			}, (int y) => ((float)y < (float)height2 * 0.35f || (float)y > (float)height2 * 0.75f) ? 0f : random.Float(0f, 0.33f * (float)height2));
			m_treeBrushesByType[1].Add(item2);
		}
		m_treeBrushesByType[2] = new List<TerrainBrush>();
		for (int num2 = 0; num2 < 16; num2++)
		{
			int[] array3 = new int[16]
			{
				7, 8, 9, 10, 10, 11, 11, 12, 12, 13,
				13, 14, 14, 15, 16, 17
			};
			int height3 = array3[num2];
			int branchesCount3 = height3 * 3;
			TerrainBrush item3 = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.Spruce), GetTreeLeavesValue(TreeType.Spruce), height3, branchesCount3, 3, delegate(int y, int round)
			{
				float num6 = MathUtils.Lerp(1.4f, 0.3f, (float)y / (float)height3);
				if (y < 3)
				{
					num6 = 0f;
				}
				if (y % 2 == 0)
				{
					num6 *= 0.3f;
				}
				return num6;
			}, delegate(int y)
			{
				if (y < 3 || (float)y > (float)height3 * 0.8f)
				{
					return 0f;
				}
				return (y % 2 == 0) ? 0f : MathUtils.Lerp(0.3f * (float)height3, 0f, MathUtils.Saturate((float)y / (float)height3));
			});
			m_treeBrushesByType[2].Add(item3);
		}
		m_treeBrushesByType[3] = new List<TerrainBrush>();
		for (int num3 = 0; num3 < 16; num3++)
		{
			int[] array4 = new int[18]
			{
				20, 21, 22, 23, 24, 24, 25, 25, 26, 26,
				27, 27, 28, 28, 29, 30, 31, 32
			};
			int height4 = array4[num3];
			int branchesCount4 = height4 * 3;
			float startHeight = (0.3f + (float)(num3 % 4) * 0.05f) * (float)height4;
			TerrainBrush item4 = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.TallSpruce), GetTreeLeavesValue(TreeType.TallSpruce), height4, branchesCount4, 3, delegate(int y, int round)
			{
				float num6 = MathUtils.Saturate((float)y / (float)height4);
				float num7 = MathUtils.Lerp(1.5f, 0f, MathUtils.Saturate((num6 - 0.6f) / 0.4f));
				if ((float)y < startHeight)
				{
					num7 = 0f;
				}
				if (y % 3 != 0 && y < height4 - 4)
				{
					num7 *= 0.2f;
				}
				return num7;
			}, delegate(int y)
			{
				float num6 = MathUtils.Saturate((float)y / (float)height4);
				if (y % 3 != 0)
				{
					return 0f;
				}
				return ((float)y < startHeight) ? ((!((float)y < startHeight - 4f)) ? (0.1f * (float)height4) : 0f) : MathUtils.Lerp(0.18f * (float)height4, 0f, MathUtils.Saturate((num6 - 0.6f) / 0.4f));
			});
			m_treeBrushesByType[3].Add(item4);
		}
		m_treeBrushesByType[4] = new List<TerrainBrush>();
		for (int num4 = 0; num4 < 16; num4++)
		{
			m_treeBrushesByType[4].Add(CreateMimosaBrush(random, MathUtils.Lerp(6f, 9f, (float)num4 / 15f)));
		}
		m_treeBrushesByType[5] = new List<TerrainBrush>();
		for (int num5 = 0; num5 < 16; num5++)
		{
			int[] array5 = new int[17]
			{
				10, 11, 11, 12, 12, 13, 13, 13, 14, 14,
				14, 15, 15, 16, 16, 17, 17
			};
			int height5 = array5[num5];
			int branchesCount5 = height5 * 3;
			TerrainBrush item5 = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.Poplar), GetTreeLeavesValue(TreeType.Poplar), height5, branchesCount5, 2, delegate(int y, int round)
			{
				int num6 = ((height5 < 14) ? 1 : 2);
				if (y < num6)
				{
					return 0f;
				}
				if (round == 0)
				{
					return 1f;
				}
				if (y == num6)
				{
					return 0f;
				}
				return (y == num6 + 1) ? 0.5f : MathUtils.LinearStep(height5 - 1, num6 + 2, y);
			}, (int y) => 0f);
			m_treeBrushesByType[5].Add(item5);
		}
	}

	public static int GetTreeTrunkValue(TreeType treeType)
	{
		return m_treeTrunksByType[(int)treeType];
	}

	public static int GetTreeLeavesValue(TreeType treeType)
	{
		return m_treeLeavesByType[(int)treeType];
	}

	public static ReadOnlyList<TerrainBrush> GetTreeBrushes(TreeType treeType)
	{
		return new ReadOnlyList<TerrainBrush>(m_treeBrushesByType[(int)treeType]);
	}

	public static int GenerateRandomPlantValue(Random random, int groundValue, int temperature, int humidity, int y)
	{
		switch (Terrain.ExtractContents(groundValue))
		{
		case 2:
		case 8:
			if (humidity >= 6)
			{
				if (!(random.Float(0f, 1f) < (float)humidity / 60f))
				{
					break;
				}
				int result = Terrain.MakeBlockValue(19, 0, TallGrassBlock.SetIsSmall(0, isSmall: false));
				if (!SubsystemWeather.IsPlaceFrozen(temperature, y))
				{
					float num = random.Float(0f, 1f);
					if (num < 0.04f)
					{
						result = Terrain.MakeBlockValue(20);
					}
					else if (num < 0.07f)
					{
						result = Terrain.MakeBlockValue(24);
					}
					else if (num < 0.09f)
					{
						result = Terrain.MakeBlockValue(25);
					}
					else if (num < 0.17f)
					{
						result = Terrain.MakeBlockValue(174, 0, RyeBlock.SetIsWild(RyeBlock.SetSize(0, 7), isWild: true));
					}
					else if (num < 0.19f)
					{
						result = Terrain.MakeBlockValue(204, 0, CottonBlock.SetIsWild(CottonBlock.SetSize(0, 2), isWild: true));
					}
				}
				return result;
			}
			if (random.Float(0f, 1f) < 0.025f)
			{
				if (random.Float(0f, 1f) < 0.2f)
				{
					return Terrain.MakeBlockValue(99, 0, 0);
				}
				return Terrain.MakeBlockValue(28, 0, 0);
			}
			break;
		case 7:
			if (humidity < 8 && random.Float(0f, 1f) < 0.01f)
			{
				if (random.Float(0f, 1f) < 0.05f)
				{
					return Terrain.MakeBlockValue(99, 0, 0);
				}
				return Terrain.MakeBlockValue(28, 0, 0);
			}
			break;
		}
		return 0;
	}

	public static TreeType? GenerateRandomTreeType(Random random, int temperature, int humidity, int y, float densityMultiplier = 1f)
	{
		TreeType? result = null;
		float num = random.Float() * CalculateTreeProbability(TreeType.Oak, temperature, humidity, y);
		float num2 = random.Float() * CalculateTreeProbability(TreeType.Birch, temperature, humidity, y);
		float num3 = random.Float() * CalculateTreeProbability(TreeType.Spruce, temperature, humidity, y);
		float num4 = random.Float() * CalculateTreeProbability(TreeType.TallSpruce, temperature, humidity, y);
		float num5 = random.Float() * CalculateTreeProbability(TreeType.Mimosa, temperature, humidity, y);
		float num6 = random.Float() * CalculateTreeProbability(TreeType.Poplar, temperature, humidity, y);
		float num7 = MathUtils.Max(MathUtils.Max(num, num2, num3, num4), num5, num6);
		if (num7 > 0f)
		{
			if (num7 == num)
			{
				result = TreeType.Oak;
			}
			if (num7 == num2)
			{
				result = TreeType.Birch;
			}
			if (num7 == num3)
			{
				result = TreeType.Spruce;
			}
			if (num7 == num4)
			{
				result = TreeType.TallSpruce;
			}
			if (num7 == num5)
			{
				result = TreeType.Mimosa;
			}
			if (num7 == num6)
			{
				result = TreeType.Poplar;
			}
		}
		if (result.HasValue && random.Bool(densityMultiplier * CalculateTreeDensity(result.Value, temperature, humidity, y)))
		{
			return result;
		}
		return null;
	}

	public static float CalculateTreeDensity(TreeType treeType, int temperature, int humidity, int y)
	{
		return treeType switch
		{
			TreeType.Oak => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			TreeType.Birch => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			TreeType.Spruce => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			TreeType.TallSpruce => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			TreeType.Mimosa => 0.04f, 
			TreeType.Poplar => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			_ => 0f, 
		};
	}

	public static float CalculateTreeProbability(TreeType treeType, int temperature, int humidity, int y)
	{
		return treeType switch
		{
			TreeType.Oak => RangeProbability(temperature, 4f, 10f, 15f, 15f) * RangeProbability(humidity, 6f, 8f, 15f, 15f) * RangeProbability(y, 0f, 0f, 82f, 87f), 
			TreeType.Birch => RangeProbability(temperature, 5f, 9f, 11f, 15f) * RangeProbability(humidity, 3f, 15f, 15f, 15f) * RangeProbability(y, 0f, 0f, 82f, 87f), 
			TreeType.Spruce => RangeProbability(temperature, 0f, 0f, 6f, 10f) * RangeProbability(humidity, 3f, 10f, 11f, 12f), 
			TreeType.TallSpruce => 0.25f * RangeProbability(temperature, 0f, 0f, 6f, 10f) * RangeProbability(humidity, 9f, 11f, 15f, 15f) * RangeProbability(y, 0f, 0f, 95f, 100f), 
			TreeType.Mimosa => RangeProbability(temperature, 2f, 4f, 12f, 15f) * RangeProbability(humidity, 0f, 0f, 4f, 6f), 
			TreeType.Poplar => RangeProbability(temperature, 4f, 8f, 12f, 15f) * RangeProbability(humidity, 3f, 15f, 15f, 15f) * RangeProbability(y, 0f, 0f, 85f, 92f), 
			_ => 0f, 
		};
	}

	private static float RangeProbability(float v, float a, float b, float c, float d)
	{
		if (v < a)
		{
			return 0f;
		}
		if (v < b)
		{
			return (v - a) / (b - a);
		}
		if (v <= c)
		{
			return 1f;
		}
		if (v <= d)
		{
			return 1f - (v - c) / (d - c);
		}
		return 0f;
	}

	private static TerrainBrush CreateTreeBrush(Random random, int woodIndex, int leavesIndex, int height, int branchesCount, int leavesRounds, Func<int, int, float> leavesProbability, Func<int, float> branchesLength)
	{
		TerrainBrush terrainBrush = new TerrainBrush();
		terrainBrush.AddRay(0, -1, 0, 0, height, 0, 1, 1, 1, woodIndex);
		for (int i = 0; i < branchesCount; i++)
		{
			int x = 0;
			int num = random.Int(0, height);
			int z = 0;
			float num2 = branchesLength(num);
			Vector3 vector = Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(0f, 0.33f), random.Float(-1f, 1f))) * num2;
			int x2 = (int)MathUtils.Round(vector.X);
			int y = num + (int)MathUtils.Round(vector.Y);
			int z2 = (int)MathUtils.Round(vector.Z);
			int cutFace = 0;
			if (MathUtils.Abs(vector.X) == MathUtils.Max(MathUtils.Abs(vector.X), MathUtils.Abs(vector.Y), MathUtils.Abs(vector.Z)))
			{
				cutFace = 1;
			}
			else if (MathUtils.Abs(vector.Y) == MathUtils.Max(MathUtils.Abs(vector.X), MathUtils.Abs(vector.Y), MathUtils.Abs(vector.Z)))
			{
				cutFace = 4;
			}
			terrainBrush.AddRay(x, num, z, x2, y, z2, 1, 1, 1, (Func<int?, int?>)((int? v) => v.HasValue ? ((int?)null) : new int?(Terrain.MakeBlockValue(woodIndex, 0, WoodBlock.SetCutFace(0, cutFace)))));
		}
		for (int num3 = 0; num3 < leavesRounds; num3++)
		{
			terrainBrush.CalculateBounds(out var min, out var max);
			for (int num4 = min.X - 1; num4 <= max.X + 1; num4++)
			{
				for (int num5 = min.Z - 1; num5 <= max.Z + 1; num5++)
				{
					for (int num6 = 1; num6 <= max.Y + 1; num6++)
					{
						float num7 = leavesProbability(num6, num3);
						if (random.Float(0f, 1f) < num7 && !terrainBrush.GetValue(num4, num6, num5).HasValue && (terrainBrush.CountNonDiagonalNeighbors(num4, num6, num5, leavesIndex) != 0 || terrainBrush.CountNonDiagonalNeighbors(num4, num6, num5, (Func<int?, int>)((int? v) => (v.HasValue && Terrain.ExtractContents(v.Value) == woodIndex) ? 1 : 0)) != 0))
						{
							terrainBrush.AddCell(num4, num6, num5, 0);
						}
					}
				}
			}
			terrainBrush.Replace(0, leavesIndex);
		}
		terrainBrush.AddCell(0, height, 0, leavesIndex);
		terrainBrush.Compile();
		return terrainBrush;
	}

	private static TerrainBrush CreateMimosaBrush(Random random, float size)
	{
		TerrainBrush terrainBrush = new TerrainBrush();
		int num = m_treeTrunksByType[4];
		int num2 = m_treeLeavesByType[4];
		terrainBrush.AddRay(0, -1, 0, 0, 0, 0, 1, 1, 1, num);
		List<Point3> list = new List<Point3>();
		float num3 = random.Float(0f, (float)Math.PI * 2f);
		for (int i = 0; i < 3; i++)
		{
			float radians = num3 + (float)i * MathUtils.DegToRad(120f);
			Vector3 vector = Vector3.Transform(Vector3.Normalize(new Vector3(1f, random.Float(1f, 1.5f), 0f)), Matrix.CreateRotationY(radians));
			int num4 = random.Int((int)(0.7f * size), (int)size);
			Point3 p = new Point3(0, 0, 0);
			Point3 item = new Point3(Vector3.Round(new Vector3(p) + vector * num4));
			terrainBrush.AddRay(p.X, p.Y, p.Z, item.X, item.Y, item.Z, 1, 1, 1, num);
			list.Add(item);
		}
		foreach (Point3 item2 in list)
		{
			float num5 = random.Float(0.3f * size, 0.45f * size);
			int num6 = (int)MathUtils.Ceiling(num5);
			for (int j = item2.X - num6; j <= item2.X + num6; j++)
			{
				for (int k = item2.Y - num6; k <= item2.Y + num6; k++)
				{
					for (int l = item2.Z - num6; l <= item2.Z + num6; l++)
					{
						int num7 = MathUtils.Abs(j - item2.X) + MathUtils.Abs(k - item2.Y) + MathUtils.Abs(l - item2.Z);
						float num8 = ((new Vector3(j, k, l) - new Vector3(item2)) * new Vector3(1f, 1.7f, 1f)).Length();
						if (num8 <= num5 && (num5 - num8 > 1f || num7 <= 2 || random.Bool(0.7f)) && !terrainBrush.GetValue(j, k, l).HasValue)
						{
							terrainBrush.AddCell(j, k, l, num2);
						}
					}
				}
			}
		}
		terrainBrush.Compile();
		return terrainBrush;
	}
}
