using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Buff;

public sealed class BuffInfo : BeanBase
{
	public const int __ID__ = -138695972;

	public string Id { get; private set; }

	public int Duration { get; private set; }

	public bool IsDebuff { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public SpriteAsset IconSmall { get; private set; }

	public SpriteAsset IconNormal { get; private set; }

	public BuffEffectType EffectType { get; private set; }

	public bool SupportScale { get; private set; }

	public BuffComponentProto[] Components { get; private set; }

	public BuffInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["duration"].IsNumber)
		{
			throw new SerializationException();
		}
		Duration = _json["duration"];
		if (!_json["is_debuff"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsDebuff = _json["is_debuff"];
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
		if (!_json["description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Description_l10n_key = _json["description"]["key"];
		if (!_json["description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Description = _json["description"]["text"];
		if (!_json["icon_small"].IsObject)
		{
			throw new SerializationException();
		}
		IconSmall = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon_small"]));
		if (!_json["icon_normal"].IsObject)
		{
			throw new SerializationException();
		}
		IconNormal = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon_normal"]));
		if (!_json["effect_type"].IsNumber)
		{
			throw new SerializationException();
		}
		EffectType = (BuffEffectType)_json["effect_type"].AsInt;
		if (!_json["support_scale"].IsBoolean)
		{
			throw new SerializationException();
		}
		SupportScale = _json["support_scale"];
		JSONNode jSONNode = _json["components"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Components = new BuffComponentProto[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			BuffComponentProto buffComponentProto = BuffComponentProto.DeserializeBuffComponentProto(child);
			Components[num++] = buffComponentProto;
		}
	}

	public BuffInfo(string id, int duration, bool is_debuff, string title, string description, SpriteAsset icon_small, SpriteAsset icon_normal, BuffEffectType effect_type, bool support_scale, BuffComponentProto[] components)
	{
		Id = id;
		Duration = duration;
		IsDebuff = is_debuff;
		Title = title;
		Description = description;
		IconSmall = icon_small;
		IconNormal = icon_normal;
		EffectType = effect_type;
		SupportScale = support_scale;
		Components = components;
	}

	public static BuffInfo DeserializeBuffInfo(JSONNode _json)
	{
		return new BuffInfo(_json);
	}

	public override int GetTypeId()
	{
		return -138695972;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		BuffComponentProto[] components = Components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
		BuffComponentProto[] components = Components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Duration:" + Duration + ",IsDebuff:" + IsDebuff + ",Title:" + Title + ",Description:" + Description + ",IconSmall:" + IconSmall?.ToString() + ",IconNormal:" + IconNormal?.ToString() + ",EffectType:" + EffectType.ToString() + ",SupportScale:" + SupportScale + ",Components:" + StringUtil.CollectionToString(Components) + ",}";
	}

	public int GetValueInGame(int value)
	{
		return EffectType switch
		{
			BuffEffectType.Health => DolocAPI.AbilitySystem.recorveryAbility.GetHealthRecovery(value), 
			BuffEffectType.Energy => DolocAPI.AbilitySystem.recorveryAbility.GetEnergyRecovery(value), 
			BuffEffectType.Spirit => DolocAPI.AbilitySystem.recorveryAbility.GetSpiritRecovery(value), 
			_ => value, 
		};
	}

	public int GetValueDiffInGame(int value)
	{
		return EffectType switch
		{
			BuffEffectType.Health => DolocAPI.AbilitySystem.recorveryAbility.GetHealthDiff(value), 
			BuffEffectType.Energy => DolocAPI.AbilitySystem.recorveryAbility.GetEnergyDiff(value), 
			BuffEffectType.Spirit => DolocAPI.AbilitySystem.recorveryAbility.GetSpiritDiff(value), 
			_ => 0, 
		};
	}
}
