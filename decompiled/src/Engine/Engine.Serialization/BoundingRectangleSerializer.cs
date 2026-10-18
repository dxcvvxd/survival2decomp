namespace Engine.Serialization;

internal class BoundingRectangleSerializer : ISerializer<BoundingRectangle>
{
	public void Serialize(InputArchive archive, ref BoundingRectangle value)
	{
		archive.Serialize("Min", ref value.Min);
		archive.Serialize("Max", ref value.Max);
	}

	public void Serialize(OutputArchive archive, BoundingRectangle value)
	{
		archive.Serialize("Min", value.Min);
		archive.Serialize("Max", value.Max);
	}
}
