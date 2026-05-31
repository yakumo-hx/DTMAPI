using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class SeedTypeInfo : BeanBase
{
	public const int __ID__ = 568677244;

	public string Id { get; private set; }

	public SpriteAsset SeedWitherAsset { get; private set; }

	public SpriteAsset PlantWitherAsset { get; private set; }

	public bool RandomSwing { get; private set; }

	public string AcidificationSeed { get; private set; }

	public SeedInfo AcidificationSeed_Ref { get; private set; }

	public bool UseRoomEffect { get; private set; }

	public SeedTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["seed_wither_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SeedWitherAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["seed_wither_asset"]));
		if (!_json["plant_wither_asset"].IsObject)
		{
			throw new SerializationException();
		}
		PlantWitherAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["plant_wither_asset"]));
		if (!_json["random_swing"].IsBoolean)
		{
			throw new SerializationException();
		}
		RandomSwing = _json["random_swing"];
		if (!_json["acidification_seed"].IsString)
		{
			throw new SerializationException();
		}
		AcidificationSeed = _json["acidification_seed"];
		if (!_json["use_room_effect"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseRoomEffect = _json["use_room_effect"];
	}

	public SeedTypeInfo(string id, SpriteAsset seed_wither_asset, SpriteAsset plant_wither_asset, bool random_swing, string acidification_seed, bool use_room_effect)
	{
		Id = id;
		SeedWitherAsset = seed_wither_asset;
		PlantWitherAsset = plant_wither_asset;
		RandomSwing = random_swing;
		AcidificationSeed = acidification_seed;
		UseRoomEffect = use_room_effect;
	}

	public static SeedTypeInfo DeserializeSeedTypeInfo(JSONNode _json)
	{
		return new SeedTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 568677244;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		AcidificationSeed_Ref = (_tables["Plant.TbSeed"] as TbSeed).GetOrDefault(AcidificationSeed);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SeedWitherAsset:" + SeedWitherAsset?.ToString() + ",PlantWitherAsset:" + PlantWitherAsset?.ToString() + ",RandomSwing:" + RandomSwing + ",AcidificationSeed:" + AcidificationSeed + ",UseRoomEffect:" + UseRoomEffect + ",}";
	}
}
