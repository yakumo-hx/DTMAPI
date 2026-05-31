using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public class DroneSlot
{
	public readonly DroneSlotProto proto;

	public Item item { get; private set; }

	public bool IsEmpty => item == null;

	public Sprite Icon => item?.uiSprite;

	public bool isLocked => !proto.LockedComponentId.IsNullOrEmpty();

	public DroneSlot(DroneSlotProto proto)
	{
		this.proto = proto;
	}

	public bool TryGetSkillId(out string skillId)
	{
		skillId = string.Empty;
		if (!(item is IDroneComponentItem droneComponentItem))
		{
			return false;
		}
		skillId = droneComponentItem.SkillId;
		return !skillId.IsNullOrEmpty();
	}

	public bool Equip(Item item)
	{
		if (isLocked)
		{
			return false;
		}
		if (!IsSuitable(item))
		{
			return false;
		}
		this.item = item;
		return true;
	}

	public void ForceEquip(Item item)
	{
		this.item = item;
	}

	public bool TakeOff(out Item item)
	{
		item = null;
		if (isLocked)
		{
			return false;
		}
		item = this.item;
		this.item = null;
		return item != null;
	}

	public bool IsSuitable(Item item)
	{
		if (item is IDroneComponentItem { IsDroneComponentValid: not false } droneComponentItem)
		{
			return droneComponentItem.ComponentType == proto.SlotType;
		}
		return false;
	}
}
