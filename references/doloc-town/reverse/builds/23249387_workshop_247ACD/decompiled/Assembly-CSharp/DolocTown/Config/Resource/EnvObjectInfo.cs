using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class EnvObjectInfo : BeanBase
{
	public const int __ID__ = 1079725946;

	public string Id { get; private set; }

	public EnvObjectType Type { get; private set; }

	public SpriteAsset SpriteAsset { get; private set; }

	public AnimatorAsset AnimatorAsset { get; private set; }

	public float Radius { get; private set; }

	public float Speed { get; private set; }

	public EnvObjectInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["type"].IsNumber)
		{
			throw new SerializationException();
		}
		Type = (EnvObjectType)_json["type"].AsInt;
		if (!_json["sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_asset"]));
		if (!_json["animator_asset"].IsObject)
		{
			throw new SerializationException();
		}
		AnimatorAsset = ExternalTypeUtil.AnimatorAssetConverter(CfgAnimatorAsset.DeserializeCfgAnimatorAsset(_json["animator_asset"]));
		if (!_json["radius"].IsNumber)
		{
			throw new SerializationException();
		}
		Radius = _json["radius"];
		if (!_json["speed"].IsNumber)
		{
			throw new SerializationException();
		}
		Speed = _json["speed"];
	}

	public EnvObjectInfo(string id, EnvObjectType type, SpriteAsset sprite_asset, AnimatorAsset animator_asset, float radius, float speed)
	{
		Id = id;
		Type = type;
		SpriteAsset = sprite_asset;
		AnimatorAsset = animator_asset;
		Radius = radius;
		Speed = speed;
	}

	public static EnvObjectInfo DeserializeEnvObjectInfo(JSONNode _json)
	{
		return new EnvObjectInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1079725946;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Type:" + Type.ToString() + ",SpriteAsset:" + SpriteAsset?.ToString() + ",AnimatorAsset:" + AnimatorAsset?.ToString() + ",Radius:" + Radius + ",Speed:" + Speed + ",}";
	}
}
