using System.Collections.Generic;

namespace Engine.Serialization;

internal class DictionarySerializer<K, V> : ISerializer<Dictionary<K, V>>
{
	public void Serialize(InputArchive archive, ref Dictionary<K, V> value)
	{
		if (value == null)
		{
			value = new Dictionary<K, V>();
		}
		else
		{
			value.Clear();
		}
		archive.SerializeDictionary(null, value);
	}

	public void Serialize(OutputArchive archive, Dictionary<K, V> value)
	{
		archive.SerializeDictionary(null, value);
	}
}
