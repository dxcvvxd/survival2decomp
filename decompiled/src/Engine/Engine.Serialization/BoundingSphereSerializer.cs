namespace Engine.Serialization;

internal class BoundingSphereSerializer : ISerializer<BoundingSphere>
{
	public void Serialize(InputArchive archive, ref BoundingSphere value)
	{
		archive.Serialize("Center", ref value.Center);
		archive.Serialize("Radius", ref value.Radius);
	}

	public void Serialize(OutputArchive archive, BoundingSphere value)
	{
		archive.Serialize("Center", value.Center);
		archive.Serialize("Radius", value.Radius);
	}
}
