using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw;

public class Blackboard : IBlackboard
{
	public IBlackboard parent;

	public readonly Dictionary<string, BlackboardValue> values = new Dictionary<string, BlackboardValue>();

	public readonly Dictionary<Type, object> typeValues = new Dictionary<Type, object>();

	public Blackboard(IBlackboard parent)
	{
		this.parent = parent;
	}

	public T GetComponent<T>() where T : MonoBehaviour
	{
		return Read<T>();
	}

	public T Read<T>()
	{
		if (typeValues.TryGetValue(typeof(T), out var value) && value != null)
		{
			return (T)value;
		}
		return default(T);
	}

	public T Read<T>(string alias)
	{
		if (values.ContainsKey(alias))
		{
			return Read<T>();
		}
		return default(T);
	}

	public void Write<T>(T value)
	{
		Type typeFromHandle = typeof(T);
		if (typeValues.ContainsKey(typeFromHandle))
		{
			typeValues[typeFromHandle] = value;
		}
		else
		{
			typeValues.Add(typeFromHandle, value);
		}
	}

	public void Write<T>(T value, string alias)
	{
		if (values.ContainsKey(alias))
		{
			values[alias].Write(value);
		}
		else
		{
			values.Add(alias, new BlackboardValue(typeof(T), value));
		}
	}

	public T ReadGlobal<T>()
	{
		if (parent != null)
		{
			return parent.Read<T>();
		}
		return default(T);
	}

	public T ReadGlobal<T>(string alias)
	{
		if (parent != null)
		{
			return parent.Read<T>(alias);
		}
		return default(T);
	}

	public void WriteGlobal<T>(T value)
	{
		parent?.Write(value);
	}

	public void WriteGlobal<T>(T value, string alias)
	{
		parent?.Write(value, alias);
	}
}
