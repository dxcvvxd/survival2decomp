using System;
using Engine;

namespace Game;

public struct Segment3(Vector3 start, Vector3 end)
{
	public Vector3 Start = start;

	public Vector3 End = end;

	public override string ToString()
	{
		_003C_003Ey__InlineArray6<object> buffer = default(_003C_003Ey__InlineArray6<object>);
		buffer[0] = Start.X;
		buffer[1] = Start.Y;
		buffer[2] = Start.Z;
		buffer[3] = End.X;
		buffer[4] = End.Y;
		buffer[5] = End.Z;
		return string.Format("{0}, {1}, {2},  {3}, {4}, {5}", (ReadOnlySpan<object?>)buffer);
	}

	public static float Distance(Segment3 s, Vector3 p)
	{
		Vector3 v = s.End - s.Start;
		Vector3 v2 = s.Start - p;
		Vector3 v3 = s.End - p;
		float num = Vector3.Dot(v2, v);
		if (num * Vector3.Dot(v3, v) <= 0f)
		{
			float num2 = v.LengthSquared();
			if (num2 == 0f)
			{
				return Vector3.Distance(p, s.Start);
			}
			return MathUtils.Sqrt(Vector3.Cross(p - s.Start, v).LengthSquared() / num2);
		}
		if (!(num > 0f))
		{
			return v3.Length();
		}
		return v2.Length();
	}

	public static Vector3 NearestPoint(Segment3 s, Vector3 p)
	{
		Vector3 v = s.End - s.Start;
		Vector3 v2 = s.Start - p;
		Vector3 v3 = s.End - p;
		float num = Vector3.Dot(v2, v);
		if (num * Vector3.Dot(v3, v) <= 0f)
		{
			float num2 = v.LengthSquared();
			if (num2 == 0f)
			{
				return s.Start;
			}
			float num3 = MathUtils.Sqrt(v2.LengthSquared() - Vector3.Cross(p - s.Start, v).LengthSquared() / num2);
			return Vector3.Lerp(s.Start, s.End, num3 / MathUtils.Sqrt(num2));
		}
		if (!(num > 0f))
		{
			return s.End;
		}
		return s.Start;
	}
}
