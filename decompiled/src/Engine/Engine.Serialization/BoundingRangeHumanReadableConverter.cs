using System;

namespace Engine.Serialization;

[HumanReadableConverter(new Type[] { typeof(BoundingRange) })]
internal class BoundingRangeHumanReadableConverter : IHumanReadableConverter
{
	public string ConvertToString(object value)
	{
		BoundingRange boundingRange = (BoundingRange)value;
		return HumanReadableConverter.ValuesListToString<float>(',', boundingRange.Min, boundingRange.Max);
	}

	public object ConvertFromString(Type type, string data)
	{
		float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
		if (array.Length == 2)
		{
			return new BoundingRange(array[0], array[1]);
		}
		throw new Exception();
	}
}
