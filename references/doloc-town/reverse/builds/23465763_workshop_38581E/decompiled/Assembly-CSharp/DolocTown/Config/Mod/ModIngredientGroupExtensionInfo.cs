using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModIngredientGroupExtensionInfo : BeanBase
{
	public const int __ID__ = -1429989899;

	public string Id { get; private set; }

	public IngredientGroupInfo Id_Ref { get; private set; }

	public string[] ExtraItems { get; private set; }

	public ItemInfo[] ExtraItems_Ref { get; private set; }

	public ModIngredientGroupExtensionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["extra_items"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ExtraItems = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			ExtraItems[num++] = text;
		}
	}

	public ModIngredientGroupExtensionInfo(string id, string[] extra_items)
	{
		Id = id;
		ExtraItems = extra_items;
	}

	public static ModIngredientGroupExtensionInfo DeserializeModIngredientGroupExtensionInfo(JSONNode _json)
	{
		return new ModIngredientGroupExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1429989899;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Recipe.TbIngredientGroup"] as TbIngredientGroup).GetOrDefault(Id);
		int num = ExtraItems.Length;
		TbItem tbItem = (TbItem)_tables["Item.TbItem"];
		ExtraItems_Ref = new ItemInfo[num];
		for (int i = 0; i < num; i++)
		{
			ExtraItems_Ref[i] = tbItem.GetOrDefault(ExtraItems[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ExtraItems:" + StringUtil.CollectionToString(ExtraItems) + ",}";
	}
}
