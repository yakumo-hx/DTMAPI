using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class PositionTypeInfo : BeanBase
{
	public const int __ID__ = 1043895219;

	public MapMissionTipType Id { get; private set; }

	public SpriteAsset Icon { get; private set; }

	public PositionTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (MapMissionTipType)_json["id"].AsInt;
		if (!_json["icon"].IsObject)
		{
			throw new SerializationException();
		}
		Icon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon"]));
	}

	public PositionTypeInfo(MapMissionTipType id, SpriteAsset icon)
	{
		Id = id;
		Icon = icon;
	}

	public static PositionTypeInfo DeserializePositionTypeInfo(JSONNode _json)
	{
		return new PositionTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1043895219;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",Icon:" + Icon?.ToString() + ",}";
	}
}
