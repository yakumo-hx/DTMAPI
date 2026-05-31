using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoLight : DroneFunctionProto
{
	public const int __ID__ = -222462424;

	public SpriteAsset LightMask { get; private set; }

	public string EntityName { get; private set; }

	public DroneFunctionProtoLight(JSONNode _json)
		: base(_json)
	{
		if (!_json["light_mask"].IsObject)
		{
			throw new SerializationException();
		}
		LightMask = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["light_mask"]));
		if (!_json["entity_name"].IsString)
		{
			throw new SerializationException();
		}
		EntityName = _json["entity_name"];
	}

	public DroneFunctionProtoLight(SpriteAsset light_mask, string entity_name)
	{
		LightMask = light_mask;
		EntityName = entity_name;
	}

	public static DroneFunctionProtoLight DeserializeDroneFunctionProtoLight(JSONNode _json)
	{
		return new DroneFunctionProtoLight(_json);
	}

	public override int GetTypeId()
	{
		return -222462424;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ LightMask:" + LightMask?.ToString() + ",EntityName:" + EntityName + ",}";
	}
}
