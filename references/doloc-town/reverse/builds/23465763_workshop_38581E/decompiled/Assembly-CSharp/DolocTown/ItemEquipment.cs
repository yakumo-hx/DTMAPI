using System.Linq;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemEquipment : Item
{
	private EquipmentInfo _equipmentProto;

	public override string title
	{
		get
		{
			if (equipmentEntity != null)
			{
				return equipmentEntity.Title;
			}
			return base.title;
		}
	}

	public override Sprite uiSprite
	{
		get
		{
			if (!(equipmentEntity?.OverrideUiSprite == null))
			{
				return equipmentEntity.OverrideUiSprite;
			}
			return base.uiSprite;
		}
	}

	[JsonProperty]
	public Equipment equipmentEntity { get; set; }

	public bool IsDirty => equipmentEntity?.IsDirty ?? false;

	public override bool noOverlay
	{
		get
		{
			if (!base.noOverlay)
			{
				return IsDirty;
			}
			return true;
		}
	}

	private BuilderTipBase BuilderTipBase => EquipmentProto.FitType switch
	{
		EquipmentFitType.UNIVERSAL => DolocAPI.dolocBuilder.GetBuilderTip<EquipmentItemBuilderTip>(), 
		EquipmentFitType.GROUND => DolocAPI.dolocBuilder.GetBuilderTip<EquipmentItemBuilderTip>(), 
		EquipmentFitType.DECAL => DolocAPI.dolocBuilder.GetBuilderTip<DecalEquipmentItemBuilderTip>(), 
		_ => null, 
	};

	public EquipmentInfo EquipmentProto
	{
		get
		{
			if (_equipmentProto == null)
			{
				DolocAPI.QueryEquipment(base.proto.Id, out _equipmentProto);
			}
			return _equipmentProto;
		}
	}

	public ItemEquipment(ItemInfo item, int count)
		: base(item, count)
	{
	}

	[JsonConstructor]
	public ItemEquipment(string itemName, int itemCount, Equipment equipmentEntity = null)
		: base(itemName, itemCount)
	{
		this.equipmentEntity = equipmentEntity;
	}

	public override Item Clone(int count)
	{
		return new ItemEquipment(name, count, equipmentEntity);
	}

	protected override void OnUseAsTool()
	{
		if (BuilderTipBase.ConfirmBuild())
		{
			DolocAPI.ReQuickSelectCurrentItem();
			DolocAPI.RefreshScanner();
		}
	}

	protected override void OnUseAsItem()
	{
		BuilderTipBase.TurnIndicator();
	}

	public override bool IsSame(Item other)
	{
		if (!base.IsSame(other) || !(other is ItemEquipment itemEquipment))
		{
			return false;
		}
		if (!IsDirty)
		{
			return !itemEquipment.IsDirty;
		}
		return false;
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		BuilderTipBase.RunBuilder(this);
	}

	protected override void OnQuickDeselect()
	{
		BuilderTipBase.ExitBuilder();
		base.OnQuickDeselect();
	}

	protected override bool CanPutInToContainer()
	{
		if (equipmentEntity is IContainer container)
		{
			return container.inventory.isEmpty;
		}
		return true;
	}

	protected override int GetSellingPrice()
	{
		if (base.proto.SellingPrice >= 0 || !DolocAPI.QueryRecipe(name, out var recipe))
		{
			return Mathf.Max(0, base.proto.SellingPrice);
		}
		int num = 0;
		CountItem[] inputItems = recipe.InputItems;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			DolocAPI.QueryItemProto(countItem.itemName, out var itemInfo);
			num += itemInfo.SellingPrice * countItem.itemCount;
		}
		return Mathf.Max(0, num);
	}

	protected override bool CanSell()
	{
		if (equipmentEntity is IContainer container)
		{
			return container.inventory.isEmpty;
		}
		return true;
	}

	protected override bool CanDispose()
	{
		if (!(equipmentEntity is IContainer { inventory: not null } container) || container.inventory.isEmpty)
		{
			return true;
		}
		return container.inventory.ReadAll().All((Item item) => item.disposable);
	}

	protected override bool CanBuyback()
	{
		return true;
	}

	public override string GetExtraInfo1()
	{
		Equipment equipment = EquipmentManager.CreateDisposeEquipment(DolocAPI.CurrentRoom, EquipmentProto, turn: false);
		if (!equipment.ExtraInfoAsItem.IsNullOrEmpty())
		{
			return equipment.ExtraInfoAsItem;
		}
		return base.GetExtraInfo1();
	}
}
