using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config;

public sealed class ArrayInt : BeanBase
{
	public const int __ID__ = -1228257546;

	public int[] Array { get; private set; }

	public ArrayInt(JSONNode _json)
	{
		JSONNode jSONNode = _json["array"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Array = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			Array[num++] = num2;
		}
	}

	public ArrayInt(int[] array)
	{
		Array = array;
	}

	public static ArrayInt DeserializeArrayInt(JSONNode _json)
	{
		return new ArrayInt(_json);
	}

	public override int GetTypeId()
	{
		return -1228257546;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Array:" + StringUtil.CollectionToString(Array) + ",}";
	}
}
