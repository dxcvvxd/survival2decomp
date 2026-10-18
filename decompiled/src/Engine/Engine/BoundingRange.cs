using System;
using System.Collections.Generic;

namespace Engine;

public struct BoundingRange : IEquatable<BoundingRange>
{
	public float Min;

	public float Max;

	public BoundingRange(float min, float max)
	{
		Min = min;
		Max = max;
	}

	public BoundingRange(IEnumerable<float> points)
	{
		if (points == null)
		{
			throw new ArgumentNullException("points");
		}
		Min = float.PositiveInfinity;
		Max = float.NegativeInfinity;
		foreach (float point in points)
		{
			Min = MathUtils.Min(Min, point);
			Max = MathUtils.Max(Max, point);
		}
		if (Min == float.PositiveInfinity)
		{
			throw new ArgumentException("points");
		}
	}

	public static implicit operator BoundingRange((float Min, float Max) v)
	{
		return new BoundingRange(v.Min, v.Max);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is BoundingRange))
		{
			return false;
		}
		return Equals((BoundingRange)obj);
	}

	public override int GetHashCode()
	{
		return Min.GetHashCode() + Max.GetHashCode();
	}

	public override string ToString()
	{
		return $"{Min},{Max}";
	}

	public bool Equals(BoundingRange other)
	{
		if (Min == other.Min)
		{
			return Max == other.Max;
		}
		return false;
	}

	public float Center()
	{
		return 0.5f * (Min + Max);
	}

	public float Size()
	{
		return Max - Min;
	}

	public bool Contains(float p)
	{
		if (p >= Min)
		{
			return p <= Max;
		}
		return false;
	}

	public bool Intersection(BoundingRange r)
	{
		if (r.Max >= Min)
		{
			return r.Min <= Max;
		}
		return false;
	}

	public static BoundingRange Intersection(BoundingRange r1, BoundingRange r2)
	{
		float num = MathUtils.Max(r1.Min, r2.Min);
		float num2 = MathUtils.Min(r1.Max, r2.Max);
		if (!(num2 > num))
		{
			return default(BoundingRange);
		}
		return new BoundingRange(num, num2);
	}

	public static BoundingRange Union(BoundingRange r1, BoundingRange r2)
	{
		float min = MathUtils.Min(r1.Min, r2.Min);
		float max = MathUtils.Max(r1.Max, r2.Max);
		return new BoundingRange(min, max);
	}

	public static BoundingRange Union(BoundingRange r, float p)
	{
		float min = MathUtils.Min(r.Min, p);
		float max = MathUtils.Max(r.Max, p);
		return new BoundingRange(min, max);
	}

	public static float Distance(BoundingRange r, float p)
	{
		return MathUtils.Max(r.Min - p, 0f, p - r.Max);
	}

	public static bool operator ==(BoundingRange a, BoundingRange b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(BoundingRange a, BoundingRange b)
	{
		return !a.Equals(b);
	}
}
