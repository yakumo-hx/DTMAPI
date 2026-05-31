using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class DismantleRecipeInfo : BeanBase
{
	public const int __ID__ = 669172029;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public int CostTime { get; private set; }

	public CountItem[] InputItems { get; private set; }

	public ItemSpawnEntry OutputItemSpawnEntry { get; private set; }

	public int TechPoint { get; private set; }

	public string RecipeTitle
	{
		get
		{
			if (!Title.IsNullOrEmpty())
			{
				return Title;
			}
			return DolocAPI.GetItemTitle(InputItems.First().itemName);
		}
	}

	public DismantleRecipeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["cost_time"].IsNumber)
		{
			throw new SerializationException();
		}
		CostTime = _json["cost_time"];
		JSONNode jSONNode = _json["input_items"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		InputItems = new CountItem[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CountItem countItem = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(child));
			InputItems[num++] = countItem;
		}
		if (!_json["output_item_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		OutputItemSpawnEntry = ItemSpawnEntry.DeserializeItemSpawnEntry(_json["output_item_spawn_entry"]);
		if (!_json["tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		TechPoint = _json["tech_point"];
	}

	public DismantleRecipeInfo(string id, string title, int cost_time, CountItem[] input_items, ItemSpawnEntry output_item_spawn_entry, int tech_point)
	{
		Id = id;
		Title = title;
		CostTime = cost_time;
		InputItems = input_items;
		OutputItemSpawnEntry = output_item_spawn_entry;
		TechPoint = tech_point;
	}

	public static DismantleRecipeInfo DeserializeDismantleRecipeInfo(JSONNode _json)
	{
		return new DismantleRecipeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 669172029;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		OutputItemSpawnEntry?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		OutputItemSpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",CostTime:" + CostTime + ",InputItems:" + StringUtil.CollectionToString(InputItems) + ",OutputItemSpawnEntry:" + OutputItemSpawnEntry?.ToString() + ",TechPoint:" + TechPoint + ",}";
	}
}
