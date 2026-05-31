using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class DishGroupInfo : BeanBase
{
	public const int __ID__ = -341353741;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string SwitchMainInfo { get; private set; }

	public string SwitchMainInfo_l10n_key { get; }

	public string SwitchSubInfo { get; private set; }

	public string SwitchSubInfo_l10n_key { get; }

	public string UnknowTitle { get; private set; }

	public string UnknowTitle_l10n_key { get; }

	public string UnknowDescription { get; private set; }

	public string UnknowDescription_l10n_key { get; }

	public SpriteAsset UnknowIcon { get; private set; }

	public float TimeRatio { get; private set; }

	public int SlotCount { get; private set; }

	public string DefaultDish { get; private set; }

	public DishInfo DefaultDish_Ref { get; private set; }

	public List<string> DishIds { get; private set; }

	public List<DishInfo> DishIds_Ref { get; private set; }

	public DishGroupInfo(JSONNode _json)
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
		if (!_json["switch_main_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SwitchMainInfo_l10n_key = _json["switch_main_info"]["key"];
		if (!_json["switch_main_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SwitchMainInfo = _json["switch_main_info"]["text"];
		if (!_json["switch_sub_info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SwitchSubInfo_l10n_key = _json["switch_sub_info"]["key"];
		if (!_json["switch_sub_info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SwitchSubInfo = _json["switch_sub_info"]["text"];
		if (!_json["unknow_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UnknowTitle_l10n_key = _json["unknow_title"]["key"];
		if (!_json["unknow_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UnknowTitle = _json["unknow_title"]["text"];
		if (!_json["unknow_description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		UnknowDescription_l10n_key = _json["unknow_description"]["key"];
		if (!_json["unknow_description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		UnknowDescription = _json["unknow_description"]["text"];
		if (!_json["unknow_icon"].IsObject)
		{
			throw new SerializationException();
		}
		UnknowIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["unknow_icon"]));
		if (!_json["time_ratio"].IsNumber)
		{
			throw new SerializationException();
		}
		TimeRatio = _json["time_ratio"];
		if (!_json["slot_count"].IsNumber)
		{
			throw new SerializationException();
		}
		SlotCount = _json["slot_count"];
		if (!_json["default_dish"].IsString)
		{
			throw new SerializationException();
		}
		DefaultDish = _json["default_dish"];
		JSONNode jSONNode = _json["dish_ids"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		DishIds = new List<string>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string item = child;
			DishIds.Add(item);
		}
	}

	public DishGroupInfo(string id, string title, string switch_main_info, string switch_sub_info, string unknow_title, string unknow_description, SpriteAsset unknow_icon, float time_ratio, int slot_count, string default_dish, List<string> dish_ids)
	{
		Id = id;
		Title = title;
		SwitchMainInfo = switch_main_info;
		SwitchSubInfo = switch_sub_info;
		UnknowTitle = unknow_title;
		UnknowDescription = unknow_description;
		UnknowIcon = unknow_icon;
		TimeRatio = time_ratio;
		SlotCount = slot_count;
		DefaultDish = default_dish;
		DishIds = dish_ids;
	}

	public static DishGroupInfo DeserializeDishGroupInfo(JSONNode _json)
	{
		return new DishGroupInfo(_json);
	}

	public override int GetTypeId()
	{
		return -341353741;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		DefaultDish_Ref = (_tables["Recipe.TbDish"] as TbDish).GetOrDefault(DefaultDish);
		TbDish tbDish = (TbDish)_tables["Recipe.TbDish"];
		DishIds_Ref = new List<DishInfo>();
		foreach (string dishId in DishIds)
		{
			DishIds_Ref.Add(tbDish.GetOrDefault(dishId));
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		SwitchMainInfo = translator(SwitchMainInfo_l10n_key, SwitchMainInfo);
		SwitchSubInfo = translator(SwitchSubInfo_l10n_key, SwitchSubInfo);
		UnknowTitle = translator(UnknowTitle_l10n_key, UnknowTitle);
		UnknowDescription = translator(UnknowDescription_l10n_key, UnknowDescription);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",SwitchMainInfo:" + SwitchMainInfo + ",SwitchSubInfo:" + SwitchSubInfo + ",UnknowTitle:" + UnknowTitle + ",UnknowDescription:" + UnknowDescription + ",UnknowIcon:" + UnknowIcon?.ToString() + ",TimeRatio:" + TimeRatio + ",SlotCount:" + SlotCount + ",DefaultDish:" + DefaultDish + ",DishIds:" + StringUtil.CollectionToString(DishIds) + ",}";
	}
}
