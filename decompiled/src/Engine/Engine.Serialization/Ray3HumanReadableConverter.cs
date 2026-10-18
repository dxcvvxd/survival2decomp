using System;

namespace Engine.Serialization;

[HumanReadableConverter(new Type[] { typeof(Ray3) })]
internal class Ray3HumanReadableConverter : IHumanReadableConverter
{
	public string ConvertToString(object value)
	{
		Ray3 ray = (Ray3)value;
		return HumanReadableConverter.ValuesListToString<float>(',', ray.Position.X, ray.Position.Y, ray.Position.Z, ray.Direction.X, ray.Direction.Y, ray.Direction.Z);
	}

	public object ConvertFromString(Type type, string data)
	{
		float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
		if (array.Length == 6)
		{
			return new Ray3(new Vector3(array[0], array[1], array[2]), new Vector3(array[3], array[4], array[5]));
		}
		throw new Exception();
	}
}
