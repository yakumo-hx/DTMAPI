using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class MissionTypeInfo : BeanBase
{
	public const int __ID__ = -826856430;

	public string Id { get; private set; }

	public int Order { get; private set; }

	public SpriteAsset UiIcon { get; private set; }

	public SpriteAsset MapIcon { get; private set; }

	public SpriteAsset MapIconBig { get; private set; }

	public string Info { get; private set; }

	public string Info_l10n_key { get; }

	public MissionTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["order"].IsNumber)
		{
			throw new SerializationException();
		}
		Order = _json["order"];
		if (!_json["ui_icon"].IsObject)
		{
			throw new SerializationException();
		}
		UiIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_icon"]));
		if (!_json["map_icon"].IsObject)
		{
			throw new SerializationException();
		}
		MapIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["map_icon"]));
		if (!_json["map_icon_big"].IsObject)
		{
			throw new SerializationException();
		}
		MapIconBig = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["map_icon_big"]));
		if (!_json["info"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Info_l10n_key = _json["info"]["key"];
		if (!_json["info"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Info = _json["info"]["text"];
	}

	public MissionTypeInfo(string id, int order, SpriteAsset ui_icon, SpriteAsset map_icon, SpriteAsset map_icon_big, string info)
	{
		Id = id;
		Order = order;
		UiIcon = ui_icon;
		MapIcon = map_icon;
		MapIconBig = map_icon_big;
		Info = info;
	}

	public static MissionTypeInfo DeserializeMissionTypeInfo(JSONNode _json)
	{
		return new MissionTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return -826856430;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Info = translator(Info_l10n_key, Info);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Order:" + Order + ",UiIcon:" + UiIcon?.ToString() + ",MapIcon:" + MapIcon?.ToString() + ",MapIconBig:" + MapIconBig?.ToString() + ",Info:" + Info + ",}";
	}
}
