using System.Collections.Generic;

namespace Engine.Serialization;

internal class HashSetSerializer<T> : ISerializer<HashSet<T>>
{
	public void Serialize(InputArchive archive, ref HashSet<T> value)
	{
		if (value == null)
		{
			value = new HashSet<T>();
		}
		else
		{
			value.Clear();
		}
		archive.SerializeCollection(null, value);
	}

	public void Serialize(OutputArchive archive, HashSet<T> value)
	{
		archive.SerializeCollection(null, null, value);
	}
}
