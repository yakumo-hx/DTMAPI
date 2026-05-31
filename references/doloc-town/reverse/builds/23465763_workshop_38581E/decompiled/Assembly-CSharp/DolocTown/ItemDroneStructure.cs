using System.Collections.Generic;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Drone;
using DolocTown.Config.Item;
using DolocTown.Config.Localization;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemDroneStructure : Item
{
	[JsonProperty]
	public readonly DroneStruct droneStructure;

	private DroneStructureInfo _structureProto;

	private static Dictionary<string, Sprite> _composedSpriteCache = new Dictionary<string, Sprite>();

	public override bool availableIfRiding => true;

	public override bool invalid
	{
		get
		{
			if (!base.invalid)
			{
				return !droneStructure.IsValid;
			}
			return true;
		}
	}

	public DroneStructureInfo structureProto => _structureProto ?? (_structureProto = DolocConfig.Tables.TbDroneStructure.GetOrDefault(base.proto.Id));

	public Sprite ComposedDroneSprite
	{
		get
		{
			if (_composedSpriteCache.TryGetValue(GetComposedSpriteKey, out var value))
			{
				return value;
			}
			Sprite sprite = BuildComposedSprite();
			_composedSpriteCache[GetComposedSpriteKey] = sprite;
			return sprite;
		}
	}

	private string GetComposedSpriteKey
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder(base.proto.Id);
			DroneSlot[] slots = droneStructure.slots;
			foreach (DroneSlot droneSlot in slots)
			{
				if (droneSlot.IsEmpty)
				{
					stringBuilder.Append("_<empty>_");
				}
				else
				{
					stringBuilder.Append("_" + droneSlot.item.proto.Id + "_");
				}
			}
			return stringBuilder.ToString();
		}
	}

	public ItemDroneStructure(ItemInfo item, int count)
		: base(item, count)
	{
		droneStructure = new DroneStruct(structureProto);
	}

	[JsonConstructor]
	protected ItemDroneStructure(string itemName, int itemCount, DroneStruct droneStructure)
		: base(itemName, itemCount)
	{
		this.droneStructure = droneStructure ?? new DroneStruct(structureProto);
	}

	protected override void OnUseAsTool()
	{
		Item oldDrone;
		if (DolocAPI.IsEquippedDrone(this))
		{
			if (DolocAPI.CurrentDrone.IsEmptyWeapon)
			{
				DolocAPI.EnterUI((DronePanelUiState state) => state.HandleStartUpArgs(this));
			}
		}
		else if (DolocAPI.EquipDrone(this, out oldDrone))
		{
			CostSelf();
			DolocAPI.PlaceItem(oldDrone);
			DolocAPI.DelayFrame(delegate
			{
				DolocAPI.uiSystem.inventoryQuick.SelectDrone();
			});
		}
	}

	protected override void OnUseAsItem()
	{
		if (DolocAPI.IsEquippedDrone(this))
		{
			DolocAPI.EnterUI((DronePanelUiState state) => state.HandleStartUpArgs(DolocAPI.archiveHandle.farmData.agentData.agentEquipment.droneItem as ItemDroneStructure));
		}
		else
		{
			DolocAPI.EnterUI((DronePanelUiState state) => state.HandleStartUpArgs(this));
		}
	}

	public override Item Clone(int count)
	{
		return new ItemDroneStructure(name, count, droneStructure);
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		DolocAPI.Broadcast(OperationEventType.SELECTED_DRONE_ITEM);
		if (DolocAPI.IsEquippedDrone(this))
		{
			DolocAPI.gameStateManager.agentController.droneController.SetSelected(value: true);
		}
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		if (DolocAPI.IsEquippedDrone(this))
		{
			DolocAPI.gameStateManager.agentController.droneController.SetSelected(value: false);
		}
	}

	public override string GetExtraInfo1()
	{
		List<string> list = new List<string>();
		TbStaticText staticTexts = DolocConfig.StaticTexts;
		Color tEXTCOLOR_STD = DolocUiColor.TEXTCOLOR_STD;
		if (structureProto.MoveSpeed > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemStructureMoveSpeedIncrease, structureProto.MoveSpeed.ToString("F1").Colored(tEXTCOLOR_STD)));
		}
		if (structureProto.PowerCapacity > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemStructurePowerCapacity, structureProto.PowerCapacity.ToString("F1").Colored(tEXTCOLOR_STD)));
		}
		if (structureProto.PowerRecv > 0f)
		{
			list.Add(DolocUtils.Format(staticTexts.ItemStructurePowerRecv, structureProto.PowerRecv.ToString("F1").Colored(tEXTCOLOR_STD)));
		}
		return string.Join("\n", list);
	}

	public override bool IsSame(Item other)
	{
		if (other == this)
		{
			return true;
		}
		if (!base.IsSame(other))
		{
			return false;
		}
		if (!(other is ItemDroneStructure itemDroneStructure))
		{
			return false;
		}
		if (!itemDroneStructure.droneStructure.IsEmpty)
		{
			return false;
		}
		return droneStructure.IsEmpty;
	}

	protected override int GetSellingPrice()
	{
		Item[] allComponents = droneStructure.GetAllComponents();
		int num = base.GetSellingPrice();
		Item[] array = allComponents;
		foreach (Item item in array)
		{
			num += item.sellingPrice;
		}
		return num;
	}

	protected override bool CanBuyback()
	{
		return droneStructure.IsEmpty;
	}

	private Sprite BuildComposedSprite()
	{
		List<Sprite> list = new List<Sprite>();
		List<Vector2Int> list2 = new List<Vector2Int>();
		list.Add(structureProto.Sprite.Asset);
		list2.Add(Vector2Int.zero);
		DroneSlot[] slots = droneStructure.slots;
		foreach (DroneSlot droneSlot in slots)
		{
			if (!droneSlot.IsEmpty && droneSlot.proto.SlotType != ComponentType.Chip && !droneSlot.isLocked)
			{
				IDroneComponentItem droneComponentItem = (IDroneComponentItem)droneSlot.item;
				Vector2Int item = droneSlot.proto.VisualPivot - droneComponentItem.ComponentPivot;
				switch (droneSlot.proto.VisualType)
				{
				case SlotVisualSuitableType.Back:
					list.Insert(0, droneComponentItem.ComponentSprite);
					list2.Insert(0, item);
					break;
				case SlotVisualSuitableType.Front:
				case SlotVisualSuitableType.Bottom:
					list.Add(droneComponentItem.ComponentSprite);
					list2.Add(item);
					break;
				}
			}
		}
		return TextureUtils.ComposeSprites(list.ToArray(), list2.ToArray());
	}
}
