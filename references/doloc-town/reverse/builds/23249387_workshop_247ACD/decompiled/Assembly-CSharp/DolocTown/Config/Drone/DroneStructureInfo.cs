using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneStructureInfo : BeanBase
{
	public const int __ID__ = -613525665;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public SpriteAsset Sprite { get; private set; }

	public AnimatorAsset Animator { get; private set; }

	public string SkillId { get; private set; }

	public float MoveSpeed { get; private set; }

	public float PowerCapacity { get; private set; }

	public float PowerRecv { get; private set; }

	public DroneSlotProto[] Slots { get; private set; }

	public bool IsValid => WeaponSlotCount == 1;

	private int WeaponSlotCount
	{
		get
		{
			if (Slots.Length != 0)
			{
				return Slots.Count((DroneSlotProto slot) => slot.SlotType == ComponentType.Weapon);
			}
			return 0;
		}
	}

	public DroneStructureInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["sprite"].IsObject)
		{
			throw new SerializationException();
		}
		Sprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite"]));
		if (!_json["animator"].IsObject)
		{
			throw new SerializationException();
		}
		Animator = ExternalTypeUtil.AnimatorAssetConverter(CfgAnimatorAsset.DeserializeCfgAnimatorAsset(_json["animator"]));
		if (!_json["skillId"].IsString)
		{
			throw new SerializationException();
		}
		SkillId = _json["skillId"];
		if (!_json["move_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MoveSpeed = _json["move_speed"];
		if (!_json["power_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerCapacity = _json["power_capacity"];
		if (!_json["power_recv"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerRecv = _json["power_recv"];
		JSONNode jSONNode = _json["slots"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Slots = new DroneSlotProto[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			DroneSlotProto droneSlotProto = DroneSlotProto.DeserializeDroneSlotProto(child);
			Slots[num++] = droneSlotProto;
		}
	}

	public DroneStructureInfo(string id, SpriteAsset sprite, AnimatorAsset animator, string skillId, float move_speed, float power_capacity, float power_recv, DroneSlotProto[] slots)
	{
		Id = id;
		Sprite = sprite;
		Animator = animator;
		SkillId = skillId;
		MoveSpeed = move_speed;
		PowerCapacity = power_capacity;
		PowerRecv = power_recv;
		Slots = slots;
	}

	public static DroneStructureInfo DeserializeDroneStructureInfo(JSONNode _json)
	{
		return new DroneStructureInfo(_json);
	}

	public override int GetTypeId()
	{
		return -613525665;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		DroneSlotProto[] slots = Slots;
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		DroneSlotProto[] slots = Slots;
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Sprite:" + Sprite?.ToString() + ",Animator:" + Animator?.ToString() + ",SkillId:" + SkillId + ",MoveSpeed:" + MoveSpeed + ",PowerCapacity:" + PowerCapacity + ",PowerRecv:" + PowerRecv + ",Slots:" + StringUtil.CollectionToString(Slots) + ",}";
	}

	public DroneSlotInfo GetSlotInfo(int index)
	{
		if (index < 0 || index >= Slots.Length)
		{
			return null;
		}
		return Slots[index].GetSlotInfo();
	}
}
