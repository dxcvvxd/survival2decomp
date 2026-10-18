using System;

namespace Engine.Serialization;

[HumanReadableConverter(new Type[] { typeof(Ray2) })]
internal class Ray2HumanReadableConverter : IHumanReadableConverter
{
	public string ConvertToString(object value)
	{
		Ray2 ray = (Ray2)value;
		return HumanReadableConverter.ValuesListToString<float>(',', ray.Position.X, ray.Position.Y, ray.Direction.X, ray.Direction.Y);
	}

	public object ConvertFromString(Type type, string data)
	{
		float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
		if (array.Length == 4)
		{
			return new Ray2(new Vector2(array[0], array[1]), new Vector2(array[2], array[3]));
		}
		throw new Exception();
	}
}
