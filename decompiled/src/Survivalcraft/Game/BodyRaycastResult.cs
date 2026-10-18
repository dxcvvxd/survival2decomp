using Engine;

namespace Game;

public struct BodyRaycastResult
{
	public Ray3 Ray;

	public ComponentBody ComponentBody;

	public float Distance;

	public Vector3 HitPoint()
	{
		return Ray.Sample(Distance);
	}
}
