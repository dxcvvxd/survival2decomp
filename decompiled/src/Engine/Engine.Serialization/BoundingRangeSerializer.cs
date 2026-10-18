namespace Engine.Serialization;

internal class BoundingRangeSerializer : ISerializer<BoundingRange>
{
	public void Serialize(InputArchive archive, ref BoundingRange value)
	{
		archive.Serialize("Min", ref value.Min);
		archive.Serialize("Max", ref value.Max);
	}

	public void Serialize(OutputArchive archive, BoundingRange value)
	{
		archive.Serialize("Min", value.Min);
		archive.Serialize("Max", value.Max);
	}
}
