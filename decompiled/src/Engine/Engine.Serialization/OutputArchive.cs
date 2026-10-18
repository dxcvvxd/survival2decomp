using System;
using System.Collections.Generic;

namespace Engine.Serialization;

public abstract class OutputArchive(int version, object context) : Archive(version, context)
{
	private int m_nextObjectId = 1;

	private Dictionary<object, int> m_idByObject = new Dictionary<object, int>();

	protected new void Reset(int version, object context)
	{
		base.Reset(version, context);
		m_idByObject.Clear();
		m_nextObjectId = 1;
	}

	public abstract void Serialize(string name, sbyte value);

	public abstract void Serialize(string name, byte value);

	public abstract void Serialize(string name, short value);

	public abstract void Serialize(string name, ushort value);

	public abstract void Serialize(string name, int value);

	public abstract void Serialize(string name, uint value);

	public abstract void Serialize(string name, long value);

	public abstract void Serialize(string name, ulong value);

	public abstract void Serialize(string name, float value);

	public abstract void Serialize(string name, double value);

	public abstract void Serialize(string name, bool value);

	public abstract void Serialize(string name, char value);

	public abstract void Serialize(string name, string value);

	public abstract void Serialize(string name, byte[] value);

	public abstract void Serialize(string name, int length, byte[] value);

	public abstract void SerializeCollection<T>(string name, Func<T, string> itemNameFunc, IEnumerable<T> collection);

	public abstract void SerializeDictionary<K, V>(string name, IDictionary<K, V> dictionary);

	public void Serialize(string name, Type type, object value)
	{
		WriteObject(name, Archive.GetSerializeData(type, allowEmptySerializer: true), value);
	}

	public void Serialize<T>(string name, T value)
	{
		WriteObject(name, Archive.GetSerializeData(typeof(T), allowEmptySerializer: true), value);
	}

	public void SerializeCollection<T>(string name, IEnumerable<T> collection)
	{
		SerializeCollection(name, null, collection);
	}

	protected abstract void WriteObjectInfo(int? objectId, bool isReference, Type runtimeType);

	protected virtual void WriteObject(string name, SerializeData staticSerializeData, object value)
	{
		if (!staticSerializeData.UseObjectInfo || !base.UseObjectInfos)
		{
			staticSerializeData.VerifySerializable();
			staticSerializeData.Write(this, value);
			return;
		}
		if (value == null)
		{
			WriteObjectInfo(0, isReference: true, null);
			return;
		}
		if (m_idByObject.TryGetValue(value, out var value2))
		{
			WriteObjectInfo(value2, isReference: true, null);
			return;
		}
		Type type = value.GetType();
		int? objectId;
		if (type == staticSerializeData.Type)
		{
			objectId = (staticSerializeData.UseObjectInfo ? new int?(m_nextObjectId++) : ((int?)null));
			WriteObjectInfo(objectId, isReference: false, null);
			staticSerializeData.VerifySerializable();
			staticSerializeData.Write(this, value);
		}
		else
		{
			SerializeData serializeData = Archive.GetSerializeData(type, allowEmptySerializer: false);
			objectId = (serializeData.UseObjectInfo ? new int?(m_nextObjectId++) : ((int?)null));
			WriteObjectInfo(objectId, isReference: false, type);
			serializeData.VerifySerializable();
			serializeData.Write(this, value);
		}
		if (objectId.HasValue)
		{
			m_idByObject.Add(value, objectId.Value);
		}
	}

	protected virtual void WriteObject<T>(string name, SerializeData staticSerializeData, T value)
	{
		if (staticSerializeData.IsValueType)
		{
			staticSerializeData.VerifySerializable();
			((SerializeData<T>)staticSerializeData).WriteGeneric(this, value);
		}
		else
		{
			WriteObject(name, staticSerializeData, (object)value);
		}
	}
}
