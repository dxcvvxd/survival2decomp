using System;

namespace Engine;

public struct BoundingSphere : IEquatable<BoundingSphere>
{
	public Vector3 Center;

	public float Radius;

	public BoundingSphere(float x, float y, float z, float radius)
	{
		Center = new Vector3(x, y, z);
		Radius = radius;
	}

	public BoundingSphere(Vector3 center, float radius)
	{
		Center = center;
		Radius = radius;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is BoundingSphere))
		{
			return false;
		}
		return Equals((BoundingSphere)obj);
	}

	public override int GetHashCode()
	{
		return Center.GetHashCode() + Radius.GetHashCode();
	}

	public bool Equals(BoundingSphere other)
	{
		if (Center == other.Center)
		{
			return Radius == other.Radius;
		}
		return false;
	}

	public override string ToString()
	{
		return $"{Center},{Radius}";
	}

	public bool Contains(Vector3 p)
	{
		return Vector3.DistanceSquared(Center, p) <= Radius * Radius;
	}

	public static bool operator ==(BoundingSphere s1, BoundingSphere s2)
	{
		return s1.Equals(s2);
	}

	public static bool operator !=(BoundingSphere s1, BoundingSphere s2)
	{
		return !s1.Equals(s2);
	}
}
