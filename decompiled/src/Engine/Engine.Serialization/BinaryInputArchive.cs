using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.Serialization;

public class BinaryInputArchive : InputArchive, IDisposable
{
	private Dictionary<int, string> m_stringIds = new Dictionary<int, string>();

	private EngineBinaryReader m_reader;

	public bool Use7BitInts = true;

	public Stream Stream => m_reader.BaseStream;

	public BinaryInputArchive(Stream stream, int version = 0, object context = null)
		: base(version, context)
	{
		m_reader = new EngineBinaryReader(stream);
	}

	public void Reset(Stream stream, int version = 0, object context = null)
	{
		m_reader = new EngineBinaryReader(stream);
		m_stringIds.Clear();
		Reset(version, context);
	}

	public override void Dispose()
	{
		Utilities.Dispose(ref m_reader);
	}

	public override void Serialize(string name, ref sbyte value)
	{
		value = m_reader.ReadSByte();
	}

	public override void Serialize(string name, ref byte value)
	{
		value = m_reader.ReadByte();
	}

	public override void Serialize(string name, ref short value)
	{
		value = m_reader.ReadInt16();
	}

	public override void Serialize(string name, ref ushort value)
	{
		value = m_reader.ReadUInt16();
	}

	public override void Serialize(string name, ref int value)
	{
		if (Use7BitInts)
		{
			value = m_reader.Read7BitEncodedInt();
		}
		else
		{
			value = m_reader.ReadInt32();
		}
	}

	public override void Serialize(string name, ref uint value)
	{
		value = m_reader.ReadUInt32();
	}

	public override void Serialize(string name, ref long value)
	{
		value = m_reader.ReadInt64();
	}

	public override void Serialize(string name, ref ulong value)
	{
		value = m_reader.ReadUInt64();
	}

	public override void Serialize(string name, ref float value)
	{
		value = m_reader.ReadSingle();
	}

	public override void Serialize(string name, ref double value)
	{
		value = m_reader.ReadDouble();
	}

	public override void Serialize(string name, ref bool value)
	{
		value = m_reader.ReadBoolean();
	}

	public override void Serialize(string name, ref char value)
	{
		value = m_reader.ReadChar();
	}

	public override void Serialize(string name, ref string value)
	{
		int num = m_reader.Read7BitEncodedInt();
		string value2;
		if (num == 0)
		{
			value = null;
		}
		else if (!m_stringIds.TryGetValue(num, out value2))
		{
			value = m_reader.ReadString();
			m_stringIds.Add(num, value);
		}
		else
		{
			value = value2;
		}
	}

	public override void Serialize(string name, ref byte[] value)
	{
		value = new byte[m_reader.Read7BitEncodedInt()];
		if (m_reader.Read(value, 0, value.Length) != value.Length)
		{
			throw new InvalidOperationException();
		}
	}

	public override void Serialize(string name, int length, ref byte[] value)
	{
		value = new byte[length];
		if (m_reader.Read(value, 0, value.Length) != length)
		{
			throw new InvalidOperationException();
		}
	}

	public override void SerializeCollection<T>(string name, ICollection<T> collection)
	{
		SerializeData serializeData = Archive.GetSerializeData(typeof(T), allowEmptySerializer: true);
		IEnumerator<T> enumerator = ((collection.Count > 0) ? collection.GetEnumerator() : null);
		int value = 0;
		Serialize(null, ref value);
		for (int i = 0; i < value; i++)
		{
			if (enumerator != null && enumerator.MoveNext())
			{
				T value2 = enumerator.Current;
				ReadObject(null, serializeData, ref value2, allowOverwriteOfExistingObject: false);
				continue;
			}
			T value3 = default(T);
			ReadObject(null, serializeData, ref value3, allowOverwriteOfExistingObject: true);
			collection.Add(value3);
			enumerator = null;
		}
	}

	public override void SerializeDictionary<K, V>(string name, IDictionary<K, V> dictionary)
	{
		SerializeData serializeData = Archive.GetSerializeData(typeof(K), allowEmptySerializer: true);
		SerializeData serializeData2 = Archive.GetSerializeData(typeof(V), allowEmptySerializer: true);
		int value = 0;
		Serialize(null, ref value);
		for (int i = 0; i < value; i++)
		{
			K value2 = default(K);
			V value3 = default(V);
			ReadObject(null, serializeData, ref value2, allowOverwriteOfExistingObject: true);
			if (dictionary.TryGetValue(value2, out var value4))
			{
				value3 = value4;
				ReadObject(null, serializeData2, ref value3, allowOverwriteOfExistingObject: false);
			}
			else
			{
				ReadObject(null, serializeData2, ref value3, allowOverwriteOfExistingObject: true);
				dictionary.Add(value2, value3);
			}
		}
	}

	protected override void ReadObjectInfo(out int? objectId, out bool isReference, out Type runtimeType)
	{
		int value = 0;
		Serialize(null, ref value);
		isReference = (value & 1) == 0;
		bool flag = (value & 2) != 0;
		if ((value & 4) != 0)
		{
			string value2 = null;
			Serialize(null, ref value2);
			runtimeType = TypeCache.FindType(value2, skipSystemAssemblies: false, throwIfNotFound: true);
		}
		else
		{
			runtimeType = null;
		}
		objectId = (flag ? new int?(value >> 4) : ((int?)null));
	}
}
