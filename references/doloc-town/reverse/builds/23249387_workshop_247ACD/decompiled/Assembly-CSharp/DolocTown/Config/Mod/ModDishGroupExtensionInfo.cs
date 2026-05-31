using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModDishGroupExtensionInfo : BeanBase
{
	public const int __ID__ = -946527746;

	public string Id { get; private set; }

	public DishGroupInfo Id_Ref { get; private set; }

	public string[] ExtraDishes { get; private set; }

	public DishInfo[] ExtraDishes_Ref { get; private set; }

	public ModDishGroupExtensionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["extra_dishes"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ExtraDishes = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			ExtraDishes[num++] = text;
		}
	}

	public ModDishGroupExtensionInfo(string id, string[] extra_dishes)
	{
		Id = id;
		ExtraDishes = extra_dishes;
	}

	public static ModDishGroupExtensionInfo DeserializeModDishGroupExtensionInfo(JSONNode _json)
	{
		return new ModDishGroupExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return -946527746;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Recipe.TbDishGroup"] as TbDishGroup).GetOrDefault(Id);
		int num = ExtraDishes.Length;
		TbDish tbDish = (TbDish)_tables["Recipe.TbDish"];
		ExtraDishes_Ref = new DishInfo[num];
		for (int i = 0; i < num; i++)
		{
			ExtraDishes_Ref[i] = tbDish.GetOrDefault(ExtraDishes[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ExtraDishes:" + StringUtil.CollectionToString(ExtraDishes) + ",}";
	}
}
