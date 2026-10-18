using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Engine.Serialization;

public class Archive : IDisposable
{
	protected delegate void ReadDelegateGeneric<T>(InputArchive archive, ref T value);

	protected delegate void WriteDelegateGeneric<T>(OutputArchive archive, T value);

	protected delegate void ReadDelegate(InputArchive archive, ref object value);

	protected delegate void WriteDelegate(OutputArchive archive, object value);

	protected class SerializeData<T> : SerializeData
	{
		internal ReadDelegateGeneric<T> ReadGeneric;

		internal WriteDelegateGeneric<T> WriteGeneric;

		internal SerializeData()
			: base(typeof(T))
		{
		}
	}

	protected class SerializeData
	{
		internal bool IsValueType;

		internal bool IsHumanReadableSupported;

		internal bool ConstructorSearched;

		internal ConstructorInfo Constructor;

		internal ReadDelegate Read;

		internal WriteDelegate Write;

		internal bool UseObjectInfo;

		internal AutoConstructMode AutoConstruct;

		public Type Type { get; internal set; }

		public bool IsSerializable => Read != null;

		internal SerializeData(Type type)
		{
			Type = type;
			IsValueType = type.GetTypeInfo().IsValueType;
			UseObjectInfo = !type.GetTypeInfo().IsValueType && type != typeof(string);
			IsHumanReadableSupported = HumanReadableConverter.IsTypeSupported(type);
		}

		internal SerializeData(bool useObjectInfo, AutoConstructMode autoConstruct)
		{
			UseObjectInfo = useObjectInfo;
			AutoConstruct = autoConstruct;
		}

		public void VerifySerializable()
		{
			if (!IsSerializable)
			{
				throw new InvalidOperationException("Type " + Type.FullName + " is not serializable. Type must have an associated ISerializer<T> or implement ISerializable.");
			}
		}

		internal void MergeOptionsFrom(SerializeData serializeData)
		{
			UseObjectInfo = serializeData.UseObjectInfo;
			AutoConstruct = serializeData.AutoConstruct;
		}

		internal SerializeData Clone()
		{
			return (SerializeData)MemberwiseClone();
		}

		internal object CreateInstance()
		{
			if (!ConstructorSearched)
			{
				ConstructorSearched = true;
				Constructor = FindConstructor(Type);
			}
			if (Constructor != null && Constructor.DeclaringType == Type)
			{
				return Activator.CreateInstance(Type, nonPublic: true);
			}
			object uninitializedObject = RuntimeHelpers.GetUninitializedObject(Type);
			if (Constructor != null)
			{
				Constructor.Invoke(uninitializedObject, null);
			}
			return uninitializedObject;
		}

		private ConstructorInfo FindConstructor(Type type)
		{
			ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, Array.Empty<ParameterModifier>());
			if (constructor == null && type.BaseType != null)
			{
				return FindConstructor(type.BaseType);
			}
			return constructor;
		}
	}

	private static HashSet<Assembly> m_scannedAssemblies = new HashSet<Assembly>();

	private static Dictionary<Type, SerializeData> m_serializeDataByType = new Dictionary<Type, SerializeData>();

	private static Dictionary<Type, SerializeData> m_pendingOptionsByType = new Dictionary<Type, SerializeData>();

	private static Dictionary<Type, TypeInfo> m_genericSerializersByType = new Dictionary<Type, TypeInfo>();

	public object Context;

	public bool UseObjectInfos { get; set; } = true;

	public int Version { get; set; }

	protected Archive(int version, object context)
	{
		Version = version;
		Context = context;
	}

	protected void Reset(int version, object context)
	{
		Version = version;
		Context = context;
	}

	public virtual void Dispose()
	{
	}

	public static bool IsTypeSerializable(Type type)
	{
		return GetSerializeData(type, allowEmptySerializer: true).Read != null;
	}

	public static void SetTypeSerializationOptions(Type type, bool useObjectInfo, AutoConstructMode autoConstruct)
	{
		lock (m_serializeDataByType)
		{
			SerializeData serializeData = new SerializeData(useObjectInfo, autoConstruct);
			if (m_serializeDataByType.TryGetValue(type, out var value))
			{
				value.MergeOptionsFrom(serializeData);
			}
			else
			{
				m_pendingOptionsByType[type] = serializeData;
			}
		}
	}

	public static object CreateInstance(Type type)
	{
		return GetSerializeData(type, allowEmptySerializer: true).CreateInstance();
	}

	protected static SerializeData GetSerializeData(Type type, bool allowEmptySerializer)
	{
		lock (m_serializeDataByType)
		{
			if (!m_serializeDataByType.TryGetValue(type, out var value))
			{
				ScanAssembliesForSerializers();
				if (!m_serializeDataByType.TryGetValue(type, out value))
				{
					value = CreateSerializeData(type);
					AddSerializeData(value);
				}
			}
			if (allowEmptySerializer || value.Read != null)
			{
				return value;
			}
			throw new InvalidOperationException($"ISerializer suitable for type \"{type.FullName}\" not found in any loaded assembly.");
		}
	}

	private static void ScanAssembliesForSerializers()
	{
		foreach (Assembly item in TypeCache.LoadedAssemblies.Where((Assembly a) => !TypeCache.IsKnownSystemAssembly(a)))
		{
			if (m_scannedAssemblies.Contains(item))
			{
				continue;
			}
			foreach (TypeInfo definedType in item.DefinedTypes)
			{
				foreach (Type implementedInterface in definedType.ImplementedInterfaces)
				{
					if (!implementedInterface.IsConstructedGenericType || !(implementedInterface.GetGenericTypeDefinition() == typeof(ISerializer<>)))
					{
						continue;
					}
					Type type = implementedInterface.GenericTypeArguments[0];
					if (type.IsGenericParameter)
					{
						continue;
					}
					if (!definedType.IsGenericType || !definedType.IsGenericTypeDefinition)
					{
						if (!m_serializeDataByType.ContainsKey(type))
						{
							SerializeData serializeData = CreateSerializeDataForSerializer(definedType, type);
							if (serializeData != null)
							{
								AddSerializeData(serializeData);
							}
						}
					}
					else if (type.GetTypeInfo().BaseType != typeof(Array) && type != typeof(Array) && !type.GetTypeInfo().IsEnum)
					{
						m_genericSerializersByType.Add(type.GetGenericTypeDefinition(), definedType);
					}
				}
			}
			m_scannedAssemblies.Add(item);
		}
	}

	private static SerializeData CreateSerializeData(Type type)
	{
		if (type.GetTypeInfo().ImplementedInterfaces.Contains<Type>(typeof(ISerializable)))
		{
			return CreateSerializeDataForSerializable(type);
		}
		if (type.IsArray)
		{
			return CreateSerializeDataForSerializer(typeof(ArraySerializer<>).MakeGenericType(type.GetElementType()).GetTypeInfo(), type);
		}
		if (type.GetTypeInfo().IsEnum)
		{
			Type enumUnderlyingType = type.GetEnumUnderlyingType();
			if (enumUnderlyingType == typeof(int) || enumUnderlyingType == typeof(uint))
			{
				return CreateSerializeDataForSerializer(typeof(Enum32Serializer<>).MakeGenericType(type).GetTypeInfo(), type);
			}
			if (enumUnderlyingType == typeof(long) || enumUnderlyingType == typeof(ulong))
			{
				return CreateSerializeDataForSerializer(typeof(Enum64Serializer<>).MakeGenericType(type).GetTypeInfo(), type);
			}
			throw new InvalidOperationException("Unsupported underlying enum type.");
		}
		if (type.GetTypeInfo().IsGenericType)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			if (m_genericSerializersByType.TryGetValue(genericTypeDefinition, out var value))
			{
				return CreateSerializeDataForSerializer(value.MakeGenericType(type.GenericTypeArguments).GetTypeInfo(), type);
			}
		}
		if (type.GetTypeInfo().BaseType != null && IsTypeSerializable(type.GetTypeInfo().BaseType))
		{
			SerializeData serializeData = GetSerializeData(type.GetTypeInfo().BaseType, allowEmptySerializer: true).Clone();
			serializeData.Type = type;
			if (serializeData.AutoConstruct == AutoConstructMode.NotSet)
			{
				serializeData.AutoConstruct = AutoConstructMode.Yes;
			}
			serializeData.ConstructorSearched = false;
			return serializeData;
		}
		return new SerializeData(type);
	}

	private static SerializeData CreateSerializeDataForSerializable(Type type)
	{
		SerializeData obj = (SerializeData)typeof(Archive).GetTypeInfo().GetDeclaredMethod("CreateSerializeDataForSerializableHelper").MakeGenericMethod(type)
			.Invoke(null, new object[0]);
		ApplySerializationOptionsAttribute(obj, type.GetTypeInfo());
		return obj;
	}

	private static SerializeData CreateSerializeDataForSerializer(TypeInfo serializerType, Type type)
	{
		MethodInfo methodInfo = serializerType.AsType().GetRuntimeMethods().FirstOrDefault(delegate(MethodInfo m)
		{
			if (m.Name != "Serialize")
			{
				return false;
			}
			ParameterInfo[] parameters = m.GetParameters();
			return parameters.Length == 2 && parameters[0].ParameterType == typeof(InputArchive) && parameters[1].ParameterType == type.MakeByRefType();
		});
		MethodInfo methodInfo2 = serializerType.AsType().GetRuntimeMethods().FirstOrDefault(delegate(MethodInfo m)
		{
			if (m.Name != "Serialize")
			{
				return false;
			}
			ParameterInfo[] parameters = m.GetParameters();
			return parameters.Length == 2 && parameters[0].ParameterType == typeof(OutputArchive) && parameters[1].ParameterType == type;
		});
		if (methodInfo != null && methodInfo2 != null)
		{
			object target = Activator.CreateInstance(serializerType.AsType());
			Type delegateType = typeof(ReadDelegateGeneric<>).MakeGenericType(type);
			Type delegateType2 = typeof(WriteDelegateGeneric<>).MakeGenericType(type);
			Delegate obj = methodInfo.CreateDelegate(delegateType, target);
			Delegate obj2 = methodInfo2.CreateDelegate(delegateType2, target);
			SerializeData obj3 = (SerializeData)typeof(Archive).GetTypeInfo().GetDeclaredMethod("CreateSerializeDataForSerializerHelper").MakeGenericMethod(type)
				.Invoke(null, new object[2] { obj, obj2 });
			ApplySerializationOptionsAttribute(obj3, serializerType);
			return obj3;
		}
		throw new InvalidOperationException("Serialization methods not found in " + serializerType.Name);
	}

	private static SerializeData CreateSerializeDataForSerializableHelper<T>() where T : ISerializable
	{
		SerializeData<T> serializeData = new SerializeData<T>();
		serializeData.ReadGeneric = delegate(InputArchive archive, ref T value)
		{
			value.Serialize(archive);
		};
		serializeData.WriteGeneric = delegate(OutputArchive archive, T value)
		{
			value.Serialize(archive);
		};
		if (serializeData.IsValueType)
		{
			serializeData.Read = delegate(InputArchive archive, ref object value)
			{
				T val = (T)value;
				val.Serialize(archive);
				value = val;
			};
		}
		else
		{
			serializeData.Read = delegate(InputArchive archive, ref object value)
			{
				((T)value/*cast due to constrained. prefix*/).Serialize(archive);
			};
		}
		serializeData.Write = delegate(OutputArchive archive, object value)
		{
			((T)value/*cast due to constrained. prefix*/).Serialize(archive);
		};
		serializeData.AutoConstruct = AutoConstructMode.Yes;
		return serializeData;
	}

	private static SerializeData CreateSerializeDataForSerializerHelper<T>(Delegate readDelegate, Delegate writeDelegate)
	{
		ReadDelegateGeneric<T> readDelegateGeneric = (ReadDelegateGeneric<T>)readDelegate;
		WriteDelegateGeneric<T> writeDelegateGeneric = (WriteDelegateGeneric<T>)writeDelegate;
		return new SerializeData<T>
		{
			ReadGeneric = (ReadDelegateGeneric<T>)readDelegate,
			WriteGeneric = (WriteDelegateGeneric<T>)writeDelegate,
			Read = delegate(InputArchive archive, ref object value)
			{
				T value2 = ((value != null) ? ((T)value) : default(T));
				readDelegateGeneric(archive, ref value2);
				value = value2;
			},
			Write = delegate(OutputArchive archive, object value)
			{
				writeDelegateGeneric(archive, (T)value);
			}
		};
	}

	private static void ApplySerializationOptionsAttribute(SerializeData serializeData, TypeInfo attributeTarget)
	{
		SerializationOptionsAttribute serializationOptionsAttribute = (SerializationOptionsAttribute)attributeTarget.GetCustomAttribute(typeof(SerializationOptionsAttribute));
		if (serializationOptionsAttribute != null)
		{
			serializeData.UseObjectInfo = serializationOptionsAttribute.UseObjectInfo;
			serializeData.AutoConstruct = serializationOptionsAttribute.AutoConstruct;
		}
	}

	private static void AddSerializeData(SerializeData serializeData)
	{
		if (m_pendingOptionsByType.TryGetValue(serializeData.Type, out var value))
		{
			serializeData.MergeOptionsFrom(value);
		}
		m_serializeDataByType.Add(serializeData.Type, serializeData);
	}
}
