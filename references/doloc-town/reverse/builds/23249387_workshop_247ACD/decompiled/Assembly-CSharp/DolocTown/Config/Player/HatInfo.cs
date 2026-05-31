using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Player;

public sealed class HatInfo : BeanBase
{
	public const int __ID__ = 498287580;

	private Dictionary<string, bool> hasAnimationAssets = new Dictionary<string, bool>();

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public string Skill { get; private set; }

	public AgentEquipmentSkillInfo Skill_Ref { get; private set; }

	public int Defense { get; private set; }

	public SpriteAsset IdleSprite { get; private set; }

	public SpriteAsset ClimbSprite { get; private set; }

	public SpriteAsset Preview { get; private set; }

	public AnimatorAsset Animator { get; private set; }

	public MaterialAsset Material { get; private set; }

	public PrefabAsset Prefab { get; private set; }

	public bool HideHair { get; private set; }

	public HatInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["skill"].IsString)
		{
			throw new SerializationException();
		}
		Skill = _json["skill"];
		if (!_json["defense"].IsNumber)
		{
			throw new SerializationException();
		}
		Defense = _json["defense"];
		if (!_json["idle_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		IdleSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["idle_sprite"]));
		if (!_json["climb_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		ClimbSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["climb_sprite"]));
		if (!_json["preview"].IsObject)
		{
			throw new SerializationException();
		}
		Preview = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["preview"]));
		if (!_json["animator"].IsObject)
		{
			throw new SerializationException();
		}
		Animator = ExternalTypeUtil.AnimatorAssetConverter(CfgAnimatorAsset.DeserializeCfgAnimatorAsset(_json["animator"]));
		if (!_json["material"].IsObject)
		{
			throw new SerializationException();
		}
		Material = ExternalTypeUtil.MaterialAssetConverter(CfgMaterialAsset.DeserializeCfgMaterialAsset(_json["material"]));
		if (!_json["prefab"].IsObject)
		{
			throw new SerializationException();
		}
		Prefab = ExternalTypeUtil.PrefabAssetConverter(CfgPrefabAsset.DeserializeCfgPrefabAsset(_json["prefab"]));
		if (!_json["hide_hair"].IsBoolean)
		{
			throw new SerializationException();
		}
		HideHair = _json["hide_hair"];
	}

	public HatInfo(string id, string skill, int defense, SpriteAsset idle_sprite, SpriteAsset climb_sprite, SpriteAsset preview, AnimatorAsset animator, MaterialAsset material, PrefabAsset prefab, bool hide_hair)
	{
		Id = id;
		Skill = skill;
		Defense = defense;
		IdleSprite = idle_sprite;
		ClimbSprite = climb_sprite;
		Preview = preview;
		Animator = animator;
		Material = material;
		Prefab = prefab;
		HideHair = hide_hair;
	}

	public static HatInfo DeserializeHatInfo(JSONNode _json)
	{
		return new HatInfo(_json);
	}

	public override int GetTypeId()
	{
		return 498287580;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		Skill_Ref = (_tables["Player.TbAgentEquipmentSkill"] as TbAgentEquipmentSkill).GetOrDefault(Skill);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Skill:" + Skill + ",Defense:" + Defense + ",IdleSprite:" + IdleSprite?.ToString() + ",ClimbSprite:" + ClimbSprite?.ToString() + ",Preview:" + Preview?.ToString() + ",Animator:" + Animator?.ToString() + ",Material:" + Material?.ToString() + ",Prefab:" + Prefab?.ToString() + ",HideHair:" + HideHair + ",}";
	}

	public bool HasAnimationAssets(string animationName)
	{
		if (hasAnimationAssets.TryGetValue(animationName, out var value))
		{
			return value;
		}
		hasAnimationAssets[animationName] = false;
		PlayerAnimationFrameInfo orDefault = DolocConfig.Tables.TbPlayerAnimationFrame.GetOrDefault(animationName);
		if (orDefault == null)
		{
			return false;
		}
		string text = (orDefault.OverrideId.IsNullOrEmpty() ? animationName : orDefault.OverrideId);
		if (text.IsNullOrEmpty())
		{
			return false;
		}
		for (int i = 0; i < orDefault.FrameCount; i++)
		{
			if (!DolocAPI.CheckAsset<Sprite>($"anim_hat_{Id}_{text}_{i}"))
			{
				return false;
			}
		}
		hasAnimationAssets[animationName] = true;
		return true;
	}
}
