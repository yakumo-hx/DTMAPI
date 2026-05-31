using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class IngredientGroupInfo : BeanBase
{
	public const int __ID__ = 1824165020;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public SpriteAsset Icon { get; private set; }

	public List<string> Items { get; private set; }

	public List<ItemInfo> Items_Ref { get; private set; }

	public IngredientGroupInfo(JSONNode _json)
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
		if (!_json["icon"].IsObject)
		{
			throw new SerializationException();
		}
		Icon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon"]));
		JSONNode jSONNode = _json["items"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		Items = new List<string>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string item = child;
			Items.Add(item);
		}
	}

	public IngredientGroupInfo(string id, string title, SpriteAsset icon, List<string> items)
	{
		Id = id;
		Title = title;
		Icon = icon;
		Items = items;
	}

	public static IngredientGroupInfo DeserializeIngredientGroupInfo(JSONNode _json)
	{
		return new IngredientGroupInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1824165020;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TbItem tbItem = (TbItem)_tables["Item.TbItem"];
		Items_Ref = new List<ItemInfo>();
		foreach (string item in Items)
		{
			Items_Ref.Add(tbItem.GetOrDefault(item));
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",Icon:" + Icon?.ToString() + ",Items:" + StringUtil.CollectionToString(Items) + ",}";
	}
}
