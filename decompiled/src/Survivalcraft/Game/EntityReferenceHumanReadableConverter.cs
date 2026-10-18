using System;
using Engine.Serialization;

namespace Game;

[HumanReadableConverter(new Type[] { typeof(EntityReference) })]
public class EntityReferenceHumanReadableConverter : IHumanReadableConverter
{
	public string ConvertToString(object value)
	{
		return ((EntityReference)value).ReferenceString;
	}

	public object ConvertFromString(Type type, string data)
	{
		return EntityReference.FromReferenceString(data);
	}
}
