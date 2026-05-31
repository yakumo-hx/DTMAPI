using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class IngredientGroupArray : BeanBase
{
	public const int __ID__ = 707282987;

	public string[] Array { get; private set; }

	public IngredientGroupInfo[] Array_Ref { get; private set; }

	public IngredientGroupArray(JSONNode _json)
	{
		JSONNode jSONNode = _json["array"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Array = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			Array[num++] = text;
		}
	}

	public IngredientGroupArray(string[] array)
	{
		Array = array;
	}

	public static IngredientGroupArray DeserializeIngredientGroupArray(JSONNode _json)
	{
		return new IngredientGroupArray(_json);
	}

	public override int GetTypeId()
	{
		return 707282987;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		int num = Array.Length;
		TbIngredientGroup tbIngredientGroup = (TbIngredientGroup)_tables["Recipe.TbIngredientGroup"];
		Array_Ref = new IngredientGroupInfo[num];
		for (int i = 0; i < num; i++)
		{
			Array_Ref[i] = tbIngredientGroup.GetOrDefault(Array[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Array:" + StringUtil.CollectionToString(Array) + ",}";
	}
}
