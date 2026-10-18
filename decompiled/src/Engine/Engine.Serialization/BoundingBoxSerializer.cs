namespace Engine.Serialization;

internal class BoundingBoxSerializer : ISerializer<BoundingBox>
{
	public void Serialize(InputArchive archive, ref BoundingBox value)
	{
		archive.Serialize("Min", ref value.Min);
		archive.Serialize("Max", ref value.Max);
	}

	public void Serialize(OutputArchive archive, BoundingBox value)
	{
		archive.Serialize("Min", value.Min);
		archive.Serialize("Max", value.Max);
	}
}
