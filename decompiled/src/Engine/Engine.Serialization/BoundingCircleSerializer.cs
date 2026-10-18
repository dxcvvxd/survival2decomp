namespace Engine.Serialization;

internal class BoundingCircleSerializer : ISerializer<BoundingCircle>
{
	public void Serialize(InputArchive archive, ref BoundingCircle value)
	{
		archive.Serialize("Center", ref value.Center);
		archive.Serialize("Radius", ref value.Radius);
	}

	public void Serialize(OutputArchive archive, BoundingCircle value)
	{
		archive.Serialize("Center", value.Center);
		archive.Serialize("Radius", value.Radius);
	}
}
