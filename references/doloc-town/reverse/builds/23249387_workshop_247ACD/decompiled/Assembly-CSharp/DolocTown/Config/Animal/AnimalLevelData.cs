using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class AnimalLevelData : BeanBase
{
	public const int __ID__ = -1233485120;

	public string SoundEvent { get; private set; }

	public AnimatorAsset Animator { get; private set; }

	public Vector2Int SpriteSize { get; private set; }

	public SpriteAsset Icon { get; private set; }

	public string DescriptionInSack { get; private set; }

	public string DescriptionInSack_l10n_key { get; }

	public int Price { get; private set; }

	public AnimalLevelData(JSONNode _json)
	{
		if (!_json["sound_event"].IsString)
		{
			throw new SerializationException();
		}
		SoundEvent = _json["sound_event"];
		if (!_json["animator"].IsObject)
		{
			throw new SerializationException();
		}
		Animator = ExternalTypeUtil.AnimatorAssetConverter(CfgAnimatorAsset.DeserializeCfgAnimatorAsset(_json["animator"]));
		if (!_json["sprite_size"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteSize = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["sprite_size"]));
		if (!_json["icon"].IsObject)
		{
			throw new SerializationException();
		}
		Icon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon"]));
		if (!_json["description_in_sack"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DescriptionInSack_l10n_key = _json["description_in_sack"]["key"];
		if (!_json["description_in_sack"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DescriptionInSack = _json["description_in_sack"]["text"];
		if (!_json["price"].IsNumber)
		{
			throw new SerializationException();
		}
		Price = _json["price"];
	}

	public AnimalLevelData(string sound_event, AnimatorAsset animator, Vector2Int sprite_size, SpriteAsset icon, string description_in_sack, int price)
	{
		SoundEvent = sound_event;
		Animator = animator;
		SpriteSize = sprite_size;
		Icon = icon;
		DescriptionInSack = description_in_sack;
		Price = price;
	}

	public static AnimalLevelData DeserializeAnimalLevelData(JSONNode _json)
	{
		return new AnimalLevelData(_json);
	}

	public override int GetTypeId()
	{
		return -1233485120;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		DescriptionInSack = translator(DescriptionInSack_l10n_key, DescriptionInSack);
	}

	public override string ToString()
	{
		return "{ SoundEvent:" + SoundEvent + ",Animator:" + Animator?.ToString() + ",SpriteSize:" + SpriteSize.ToString() + ",Icon:" + Icon?.ToString() + ",DescriptionInSack:" + DescriptionInSack + ",Price:" + Price + ",}";
	}
}
