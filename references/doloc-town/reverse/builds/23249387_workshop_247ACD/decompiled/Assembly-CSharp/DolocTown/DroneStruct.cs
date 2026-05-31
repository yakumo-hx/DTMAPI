using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Drone;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DroneStruct
{
	public readonly DroneStructureInfo proto;

	public readonly DroneSlot[] slots;

	public DroneStructureParams droneParams;

	public bool HasComponentChanged { get; private set; }

	[JsonProperty]
	public float power { get; private set; }

	[JsonProperty]
	public string protoName => proto.Id;

	[JsonProperty("items")]
	public Item[] _m_components
	{
		get
		{
			Item[] array = new Item[slots.Length];
			for (int i = 0; i < slots.Length; i++)
			{
				array[i] = slots[i].item;
			}
			return array;
		}
	}

	public bool IsValid => proto != null;

	public List<Item> uselessItems { get; private set; }

	public float Power => power;

	public float PowerProcess => power / droneParams.PowerCapacity;

	public bool IsFull => power >= droneParams.PowerCapacity;

	public bool IsNotFull => power < droneParams.PowerCapacity;

	public bool IsEmpty => slots.All((DroneSlot x) => x.IsEmpty);

	public DroneChipInfo[] AllChips
	{
		get
		{
			List<DroneChipInfo> list = new List<DroneChipInfo>();
			DroneSlot[] array = slots;
			foreach (DroneSlot droneSlot in array)
			{
				if (!droneSlot.IsEmpty && droneSlot.proto.SlotType == ComponentType.Chip && droneSlot.item is ItemDroneChip { chipProto: not null } itemDroneChip)
				{
					list.Add(itemDroneChip.chipProto);
				}
			}
			return list.ToArray();
		}
	}

	private DroneEngineInfo[] AllEngines
	{
		get
		{
			List<DroneEngineInfo> list = new List<DroneEngineInfo>();
			DroneSlot[] array = slots;
			foreach (DroneSlot droneSlot in array)
			{
				if (!droneSlot.IsEmpty && droneSlot.proto.SlotType == ComponentType.Engine && droneSlot.item is ItemDroneEngine { engineProto: not null } itemDroneEngine)
				{
					list.Add(itemDroneEngine.engineProto);
				}
			}
			return list.ToArray();
		}
	}

	public DroneStruct(DroneStructureInfo proto)
	{
		this.proto = proto;
		slots = InitSlots(proto);
		droneParams = new DroneStructureParams(proto.PowerCapacity, proto.PowerRecv, proto.MoveSpeed);
		power = droneParams.PowerCapacity;
	}

	[JsonConstructor]
	protected DroneStruct(string protoName, Item[] items, float power)
	{
		proto = DolocConfig.Tables.TbDroneStructure.GetOrDefault(protoName);
		if (proto != null)
		{
			this.power = power;
			slots = InitSlots(proto);
			uselessItems = RestoreItems(items);
		}
	}

	private List<Item> RestoreItems(Item[] items)
	{
		List<Item> list = new List<Item>();
		Queue<Item> queue = new Queue<Item>(items.Where((Item item) => item != null && !item.invalid));
		while (queue.Count > 0)
		{
			Item item2 = queue.Dequeue();
			if (item2 == null)
			{
				continue;
			}
			bool flag = false;
			DroneSlot[] array = slots;
			foreach (DroneSlot droneSlot in array)
			{
				if (droneSlot.IsEmpty && droneSlot.Equip(item2))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(item2);
			}
		}
		return list;
	}

	public void AfterLoadData()
	{
		foreach (Item uselessItem in uselessItems)
		{
			DolocAPI.SendItemAsEmail(uselessItem.name, 1);
		}
	}

	private DroneSlot[] InitSlots(DroneStructureInfo proto)
	{
		DroneSlot[] array = new DroneSlot[proto.Slots.Length];
		for (int i = 0; i < proto.Slots.Length; i++)
		{
			DroneSlot droneSlot = new DroneSlot(proto.Slots[i]);
			if (droneSlot.isLocked && !droneSlot.proto.LockedComponentId.IsNullOrEmpty())
			{
				Item item = DolocAPI.GenerateItem(droneSlot.proto.LockedComponentId);
				if (item != null && droneSlot.IsSuitable(item))
				{
					droneSlot.ForceEquip(item);
				}
			}
			array[i] = droneSlot;
		}
		return array;
	}

	private void SetPower(float power)
	{
		this.power = Mathf.Clamp(power, 0f, droneParams.PowerCapacity);
	}

	public bool CostPower(float cost)
	{
		if (power < cost)
		{
			return false;
		}
		power -= cost;
		return true;
	}

	public void Charge(float addition)
	{
		SetPower(power + addition);
	}

	public void ChargeToFull()
	{
		SetPower(droneParams.PowerCapacity);
	}

	public void InitializeParams(out float moveSpeed)
	{
		droneParams = proto.GetDroneParams(AllEngines);
		moveSpeed = droneParams.MoveSpeed;
		power = Mathf.Clamp(power, 0f, droneParams.PowerCapacity);
	}

	public Item[] GetAllComponents()
	{
		if (IsEmpty)
		{
			return Array.Empty<Item>();
		}
		return (from x in slots
			where x.item != null
			select x.item).ToArray();
	}

	public Item GetComponent(int index)
	{
		if (index < 0 || index >= slots.Length)
		{
			return null;
		}
		return slots[index].item;
	}

	public Item GetComponent(int index, out bool isLocked)
	{
		isLocked = false;
		if (index < 0 || index > slots.Length)
		{
			return null;
		}
		isLocked = slots[index].isLocked;
		return slots[index].item;
	}

	public bool CanEquip(Item item)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (CanEquip(i, item))
			{
				return true;
			}
		}
		return false;
	}

	public int[] GetEquipIndexes(Item item)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < slots.Length; i++)
		{
			if (CanEquip(i, item))
			{
				list.Add(i);
			}
		}
		return list.ToArray();
	}

	public bool IsSlotEmpty(int index)
	{
		if (index < 0 || index >= slots.Length)
		{
			return false;
		}
		return slots[index].IsEmpty;
	}

	public bool CanEquip(int index, Item item, out string reason)
	{
		reason = string.Empty;
		if (index < 0 || index >= slots.Length)
		{
			return false;
		}
		if (slots[index].isLocked)
		{
			reason = DolocConfig.StaticTexts.DronePanelErrLocked;
			return false;
		}
		if (!slots[index].IsSuitable(item))
		{
			reason = DolocConfig.StaticTexts.DronePanelErrLoad;
			return false;
		}
		return true;
	}

	public bool CanEquip(int index, Item item)
	{
		string reason;
		return CanEquip(index, item, out reason);
	}

	public bool Equip(int index, Item item)
	{
		if (item == null || index < 0 || index >= slots.Length)
		{
			return false;
		}
		if (!slots[index].Equip(item))
		{
			return false;
		}
		HasComponentChanged = true;
		return true;
	}

	public bool TryTakeOff(int index, out Item item)
	{
		item = null;
		if (index < 0 || index >= slots.Length)
		{
			return false;
		}
		if (!slots[index].TakeOff(out item))
		{
			return false;
		}
		HasComponentChanged = true;
		return true;
	}

	public Item TakeOff(int index)
	{
		if (index < 0 || index >= slots.Length)
		{
			return null;
		}
		if (!slots[index].TakeOff(out var item))
		{
			return null;
		}
		HasComponentChanged = true;
		return item;
	}

	public bool IsComponentChanged()
	{
		if (HasComponentChanged)
		{
			HasComponentChanged = false;
			return true;
		}
		return false;
	}

	public Vector2 GetWeaponSlotPosition(Vector2 droneSpriteSize)
	{
		DroneSlot[] array = slots;
		foreach (DroneSlot droneSlot in array)
		{
			if (droneSlot.proto.SlotType == ComponentType.Weapon)
			{
				float x = ((float)droneSlot.proto.VisualPivot.x - droneSpriteSize.x * 0.5f) * 0.125f;
				float y = ((float)droneSlot.proto.VisualPivot.y - droneSpriteSize.y * 0.5f) * 0.125f;
				Vector2 vector = new Vector2(x, y);
				if (droneSlot.IsEmpty)
				{
					return vector;
				}
				DroneWeaponInfo orDefault = DolocConfig.Tables.TbDroneWeapon.GetOrDefault(droneSlot.item.name);
				if (orDefault != null)
				{
					return new Vector2(orDefault.BulletOffset.x, orDefault.BulletOffset.y) * 0.125f + vector;
				}
				Debug.LogError("无法找到对应的武器组件\"" + droneSlot.item.name + "\"");
				return vector;
			}
		}
		return Vector2.zero;
	}

	public DroneWeaponInfo GetWeaponProto()
	{
		DroneSlot[] array = slots;
		foreach (DroneSlot droneSlot in array)
		{
			if (droneSlot.proto.SlotType == ComponentType.Weapon)
			{
				if (droneSlot.IsEmpty)
				{
					Debug.LogWarning("无人机框架的武器槽为空");
					return null;
				}
				DroneWeaponInfo orDefault = DolocConfig.Tables.TbDroneWeapon.GetOrDefault(droneSlot.item.name);
				if (orDefault == null)
				{
					Debug.LogError("无法找到对应的武器组件\"" + droneSlot.item.name + "\"");
					return null;
				}
				return orDefault;
			}
		}
		return null;
	}
}
