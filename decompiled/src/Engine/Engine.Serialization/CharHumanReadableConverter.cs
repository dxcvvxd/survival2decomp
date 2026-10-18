using System;

namespace Engine.Serialization;

[HumanReadableConverter(new Type[] { typeof(char) })]
internal class CharHumanReadableConverter : IHumanReadableConverter
{
	public string ConvertToString(object value)
	{
		return ((char)value).ToString();
	}

	public object ConvertFromString(Type type, string data)
	{
		return data[0];
	}
}
