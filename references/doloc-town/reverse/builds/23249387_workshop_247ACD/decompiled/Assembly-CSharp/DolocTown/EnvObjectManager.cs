using System;
using System.Reflection;
using DolocTown.Config.Resource;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class EnvObjectManager : DataManager<EnvObject>
{
	private static readonly Type[] EnvObjectTypes = LoadResourceEntityTypes();

	private static Type[] LoadResourceEntityTypes()
	{
		Type[] array = new Type[Enum.GetValues(typeof(EnvObjectType)).Length];
		Type[] array2 = ReflectionUtils.FindTypesWithAttributeInExecutingAsm<EnvObjectAttribute>(BindingFlags.Public | BindingFlags.NonPublic);
		Type typeFromHandle = typeof(EnvObjectAttribute);
		Type[] array3 = array2;
		foreach (Type type in array3)
		{
			EnvObjectAttribute envObjectAttribute = (EnvObjectAttribute)Attribute.GetCustomAttribute(type, typeFromHandle);
			array[(byte)envObjectAttribute.type] = type;
		}
		return array;
	}

	public EnvObject CreateEnvObject(EnvObjectInfo proto, Vector2 position)
	{
		Type type = EnvObjectTypes[(int)proto.Type];
		if (type == null)
		{
			return null;
		}
		EnvObject envObject = (EnvObject)Activator.CreateInstance(type, proto, position);
		AddData(envObject);
		return envObject;
	}

	public bool RemoveEnvObject(EnvObject obj)
	{
		return RemoveData(obj);
	}
}
