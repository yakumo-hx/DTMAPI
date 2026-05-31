using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using DolocTown.GameData;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class DroneWeaponInfo : BeanBase
{
	public const int __ID__ = 813965068;

	private bool _isBulletLoaded;

	private BulletProto _bulletProto;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public WeaponType Type { get; private set; }

	public SpriteAsset Sprite { get; private set; }

	public Vector2Int SpritePivot { get; private set; }

	public string AttackEffects { get; private set; }

	public string AttackSoundEvent { get; private set; }

	public string ExtraRenderer { get; private set; }

	public int Attack { get; private set; }

	public float CriticalRate { get; private set; }

	public float AttackSpeed { get; private set; }

	public float Accuracy { get; private set; }

	public float PowerCost { get; private set; }

	public float AttackDistance { get; private set; }

	public float MoveSpeed { get; private set; }

	public float ReloadDuration { get; private set; }

	public string WeaponSkill { get; private set; }

	public int ClipCapacity { get; private set; }

	public int ExtraBullets { get; private set; }

	public float ExtraSectorAngle { get; private set; }

	public string BulletId { get; private set; }

	public string BulletMoverId { get; private set; }

	public Vector2Int BulletOffset { get; private set; }

	public WeaponFunction Function { get; private set; }

	public BulletProto BulletProto
	{
		get
		{
			if (_isBulletLoaded)
			{
				return _bulletProto;
			}
			_isBulletLoaded = true;
			_bulletProto = DolocAPI.assets.bullets.GetData(BulletId);
			return _bulletProto;
		}
	}

	public DroneWeaponInfo(JSONNode _json)
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
		Type = (WeaponType)_json["type"].AsInt;
		if (!_json["sprite"].IsObject)
		{
			throw new SerializationException();
		}
		Sprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite"]));
		if (!_json["sprite_pivot"].IsObject)
		{
			throw new SerializationException();
		}
		SpritePivot = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["sprite_pivot"]));
		if (!_json["attack_effects"].IsString)
		{
			throw new SerializationException();
		}
		AttackEffects = _json["attack_effects"];
		if (!_json["attack_sound_event"].IsString)
		{
			throw new SerializationException();
		}
		AttackSoundEvent = _json["attack_sound_event"];
		if (!_json["extra_renderer"].IsString)
		{
			throw new SerializationException();
		}
		ExtraRenderer = _json["extra_renderer"];
		if (!_json["attack"].IsNumber)
		{
			throw new SerializationException();
		}
		Attack = _json["attack"];
		if (!_json["critical_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		CriticalRate = _json["critical_rate"];
		if (!_json["attack_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		AttackSpeed = _json["attack_speed"];
		if (!_json["accuracy"].IsNumber)
		{
			throw new SerializationException();
		}
		Accuracy = _json["accuracy"];
		if (!_json["power_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerCost = _json["power_cost"];
		if (!_json["attack_distance"].IsNumber)
		{
			throw new SerializationException();
		}
		AttackDistance = _json["attack_distance"];
		if (!_json["move_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MoveSpeed = _json["move_speed"];
		if (!_json["reload_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		ReloadDuration = _json["reload_duration"];
		if (!_json["weapon_skill"].IsString)
		{
			throw new SerializationException();
		}
		WeaponSkill = _json["weapon_skill"];
		if (!_json["clip_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		ClipCapacity = _json["clip_capacity"];
		if (!_json["extra_bullets"].IsNumber)
		{
			throw new SerializationException();
		}
		ExtraBullets = _json["extra_bullets"];
		if (!_json["extra_sector_angle"].IsNumber)
		{
			throw new SerializationException();
		}
		ExtraSectorAngle = _json["extra_sector_angle"];
		if (!_json["bullet_id"].IsString)
		{
			throw new SerializationException();
		}
		BulletId = _json["bullet_id"];
		if (!_json["bullet_mover_id"].IsString)
		{
			throw new SerializationException();
		}
		BulletMoverId = _json["bullet_mover_id"];
		if (!_json["bullet_offset"].IsObject)
		{
			throw new SerializationException();
		}
		BulletOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["bullet_offset"]));
		if (!_json["function"].IsObject)
		{
			throw new SerializationException();
		}
		Function = WeaponFunction.DeserializeWeaponFunction(_json["function"]);
	}

	public DroneWeaponInfo(string id, WeaponType type, SpriteAsset sprite, Vector2Int sprite_pivot, string attack_effects, string attack_sound_event, string extra_renderer, int attack, float critical_rate, float attack_speed, float accuracy, float power_cost, float attack_distance, float move_speed, float reload_duration, string weapon_skill, int clip_capacity, int extra_bullets, float extra_sector_angle, string bullet_id, string bullet_mover_id, Vector2Int bullet_offset, WeaponFunction function)
	{
		Id = id;
		Type = type;
		Sprite = sprite;
		SpritePivot = sprite_pivot;
		AttackEffects = attack_effects;
		AttackSoundEvent = attack_sound_event;
		ExtraRenderer = extra_renderer;
		Attack = attack;
		CriticalRate = critical_rate;
		AttackSpeed = attack_speed;
		Accuracy = accuracy;
		PowerCost = power_cost;
		AttackDistance = attack_distance;
		MoveSpeed = move_speed;
		ReloadDuration = reload_duration;
		WeaponSkill = weapon_skill;
		ClipCapacity = clip_capacity;
		ExtraBullets = extra_bullets;
		ExtraSectorAngle = extra_sector_angle;
		BulletId = bullet_id;
		BulletMoverId = bullet_mover_id;
		BulletOffset = bullet_offset;
		Function = function;
	}

	public static DroneWeaponInfo DeserializeDroneWeaponInfo(JSONNode _json)
	{
		return new DroneWeaponInfo(_json);
	}

	public override int GetTypeId()
	{
		return 813965068;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		Function?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Function?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Type:" + Type.ToString() + ",Sprite:" + Sprite?.ToString() + ",SpritePivot:" + SpritePivot.ToString() + ",AttackEffects:" + AttackEffects + ",AttackSoundEvent:" + AttackSoundEvent + ",ExtraRenderer:" + ExtraRenderer + ",Attack:" + Attack + ",CriticalRate:" + CriticalRate + ",AttackSpeed:" + AttackSpeed + ",Accuracy:" + Accuracy + ",PowerCost:" + PowerCost + ",AttackDistance:" + AttackDistance + ",MoveSpeed:" + MoveSpeed + ",ReloadDuration:" + ReloadDuration + ",WeaponSkill:" + WeaponSkill + ",ClipCapacity:" + ClipCapacity + ",ExtraBullets:" + ExtraBullets + ",ExtraSectorAngle:" + ExtraSectorAngle + ",BulletId:" + BulletId + ",BulletMoverId:" + BulletMoverId + ",BulletOffset:" + BulletOffset.ToString() + ",Function:" + Function?.ToString() + ",}";
	}
}
