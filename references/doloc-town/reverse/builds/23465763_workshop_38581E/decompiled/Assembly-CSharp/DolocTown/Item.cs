using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Recipe;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[Item]
public class Item : ItemBase
{
	protected bool allowBuildEquipment => DolocAPI.CurrentRoom?.RoomConstructInfo.AllowBuildEquipment ?? false;

	protected bool allowBuildBuilding => DolocAPI.CurrentRoom?.RoomConstructInfo.AllowBuildBuilding ?? false;

	protected bool allowBuildResource => DolocAPI.CurrentRoom?.RoomConstructInfo.AllowBuildResource ?? false;

	protected bool allowPlaceAnimal
	{
		get
		{
			if (DolocAPI.CurrentRoom is TemplateRoomInHouse templateRoomInHouse)
			{
				return templateRoomInHouse.Building.proto.IsAnimalBuilding;
			}
			return false;
		}
	}

	protected bool allowAnimalAppear
	{
		get
		{
			if (DolocAPI.CurrentRoom != DolocAPI.archiveHandle.MainFarm)
			{
				if (DolocAPI.CurrentRoom is TemplateRoomInHouse templateRoomInHouse)
				{
					return templateRoomInHouse.Building.proto.IsAnimalBuilding;
				}
				return false;
			}
			return true;
		}
	}

	protected AgentCellTip cellTip => DolocAPI.uiSystem.basicTip.AgentCellTip;

	protected Equipment SelectedEquipment => ((IEquipmentHost)DolocAPI.CurrentRoom)?.GetEquipment(cellTip.CellAnchor);

	protected Equipment SelectedEquipmentSource => ((IEquipmentHost)DolocAPI.CurrentRoom)?.GetEquipment(cellTip.CellAnchorSource);

	protected Equipment[] SelectedEquipments
	{
		get
		{
			IEquipmentHost currentRoom = DolocAPI.CurrentRoom;
			if (currentRoom == null)
			{
				return Array.Empty<Equipment>();
			}
			List<Equipment> list = new List<Equipment>();
			Vector2Int cellAnchor = cellTip.CellAnchor;
			for (int i = 0; i < cellTip.CellSize.x; i++)
			{
				Vector2Int cellpos = cellAnchor + new Vector2Int(i, 0);
				Equipment equipment = currentRoom.GetEquipment(cellpos);
				if (equipment != null && !list.Contains(equipment))
				{
					list.Add(equipment);
				}
			}
			return list.ToArray();
		}
	}

	protected Building SelectedBuilding => ((IBuildingHost)DolocAPI.CurrentRoom)?.GetBuilding(cellTip.CellAnchor);

	public ItemInfo proto { get; private set; }

	[DebugInfo("道具ID")]
	public override string name => proto.Id;

	public float sortingOrder => DolocAPI.GetItemSortingOrder(name);

	public virtual bool invalid => proto == null;

	[DebugInfo("道具图标")]
	public override Sprite uiSprite => proto.UiSpriteAsset.Asset;

	[DebugInfo("购买单价")]
	public int buyingPrice => proto.BuyingPrice;

	[DebugInfo("售出单价")]
	public int sellingPrice => GetSellingPrice();

	public int overlay => proto.Overlay;

	public virtual bool noOverlay => proto.Overlay == 1;

	public virtual string title => proto.Title;

	public virtual string description => proto.Description;

	public ItemMainTypeInfo type => subType.MainType_Ref;

	public string typeText => type.Title;

	public virtual ItemSubTypeInfo subType => proto.SubType_Ref;

	public string subTypeText => subType.Title;

	public virtual ItemAutomationTypeInfo automationType => subType.AutomationType_Ref;

	public string automationTypeText => automationType.Title;

	public bool salable
	{
		get
		{
			if (proto.Salable)
			{
				return CanSell();
			}
			return false;
		}
	}

	public bool cookable
	{
		get
		{
			if (!proto.Cookable)
			{
				return CanCook();
			}
			return true;
		}
	}

	public bool disposable
	{
		get
		{
			if (proto.Disposable && !DolocAPI.DisableDisposeItem)
			{
				return CanDispose();
			}
			return false;
		}
	}

	public bool canBuyback
	{
		get
		{
			if (salable && !DolocAPI.GlobalParameter.ItemsCanNotBuyback.Contains(name))
			{
				return CanBuyback();
			}
			return false;
		}
	}

	public bool canPutInToContainer => CanPutInToContainer();

	public virtual bool availableIfRiding => false;

	public bool isQuickSelected { get; private set; }

	protected virtual void RefreshCellTip()
	{
	}

	protected void ShowCellTip(Vector2Int offset, Vector2Int tilesize, bool flipWhenFaceLeft, bool updatePerSec = true)
	{
		if (!DolocAPI.IsAgentRiding)
		{
			cellTip.Show(offset, tilesize, flipWhenFaceLeft, RefreshCellTip, updatePerSec);
			RefreshCellTip();
		}
	}

	public void HideCellTip()
	{
		cellTip.Hide();
	}

	protected bool TryGetSelectedEquipment(out Equipment equipment)
	{
		equipment = SelectedEquipment;
		return equipment != null;
	}

	protected bool TryGetSelectedEquipment<T>(out T equipment) where T : Equipment
	{
		equipment = SelectedEquipment as T;
		return equipment != null;
	}

	protected bool TryGetSelectedEquipmentFeature<T>(out T equipment) where T : class
	{
		if (SelectedEquipment is T val)
		{
			equipment = val;
			return true;
		}
		equipment = null;
		return false;
	}

	protected bool TryGetSelectedBuilding(out Building building, bool includeInhouseRoom = false)
	{
		building = SelectedBuilding;
		if (building != null)
		{
			return true;
		}
		if (!includeInhouseRoom)
		{
			return false;
		}
		if (DolocAPI.CurrentRoom is TemplateRoomInHouse templateRoomInHouse)
		{
			building = templateRoomInHouse.Building;
			return true;
		}
		return false;
	}

	protected virtual int GetSellingPrice()
	{
		if (!(this is IContainer { inventory: not null } container) || container.inventory.isEmpty)
		{
			return proto.SellingPrice;
		}
		return proto.SellingPrice + container.inventory.ReadAll().Sum((Item item) => item.GetSellingPrice());
	}

	protected virtual bool CanSell()
	{
		if (this is IContainer { inventory: not null } container)
		{
			return container.inventory.isEmpty;
		}
		return true;
	}

	protected virtual bool CanCook()
	{
		if (!DolocConfig.Tables.TbRecipe.CookableItems.Contains(name))
		{
			return DolocConfig.Tables.TbIngredientGroup.DataList.Any((IngredientGroupInfo x) => x.Items.Contains(name));
		}
		return true;
	}

	protected virtual bool CanDispose()
	{
		if (!(this is IContainer { inventory: not null } container) || container.inventory.isEmpty)
		{
			return true;
		}
		return container.inventory.ReadAll().All((Item item) => item.disposable);
	}

	protected virtual bool CanBuyback()
	{
		if (this is IContainer { inventory: not null } container)
		{
			return container.inventory.isEmpty;
		}
		return true;
	}

	protected virtual bool CanPutInToContainer()
	{
		if (this is IContainer container)
		{
			return container.inventory.isEmpty;
		}
		return true;
	}

	public Item(ItemInfo proto, int count)
		: base(count)
	{
		this.proto = proto;
		base.count = count;
	}

	[JsonConstructor]
	protected Item(string itemName, int itemCount)
	{
		if (!DolocAPI.QueryItemProto(itemName, out var itemInfo))
		{
			Debug.LogError("item: " + itemName + "没有找到对应的配置数据");
			return;
		}
		proto = itemInfo;
		count = itemCount;
	}

	public void QuickSelect()
	{
		if (!isQuickSelected)
		{
			isQuickSelected = true;
			OnQuickSelect();
		}
	}

	public void QuickDeselect()
	{
		if (isQuickSelected)
		{
			isQuickSelected = false;
			OnQuickDeselect();
		}
	}

	protected virtual void OnQuickSelect()
	{
	}

	protected virtual void OnQuickDeselect()
	{
	}

	public void UseAsItem()
	{
		if (ConditionCheck())
		{
			OnUseAsItem();
		}
	}

	public void UseAsTool()
	{
		if (ConditionCheck())
		{
			OnUseAsTool();
		}
	}

	private bool ConditionCheck()
	{
		if (!DolocAPI.agent.IsCurrentStateSupportUseItem || DolocAPI.userInput.CurrentState.DisableUseItem)
		{
			return false;
		}
		if (DolocAPI.IsAgentRiding && !DolocAPI.SelectedItem.availableIfRiding)
		{
			if (!(proto.Function is ItemFunction))
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrCannotUseIfRiding);
			}
			return false;
		}
		if (TryEquipActiveItem())
		{
			return false;
		}
		return true;
	}

	private bool TryEquipActiveItem()
	{
		if (this is IActiveItem && !DolocAPI.IsEquippedActive(this) && DolocAPI.EquipActiveItem(this, out var oldItem))
		{
			CostSelf();
			DolocAPI.PlaceItem(oldItem);
			DolocAPI.DelayFrame(delegate
			{
				DolocAPI.uiSystem.inventoryQuick.SelectActiveItem();
			});
			return true;
		}
		return false;
	}

	protected virtual void OnUseAsItem()
	{
	}

	protected virtual void OnUseAsTool()
	{
	}

	public virtual bool IsSame(Item other)
	{
		return name == other?.name;
	}

	public int TestCombine(int compose)
	{
		int num = compose + count;
		if (num > overlay)
		{
			return num - overlay;
		}
		return 0;
	}

	public int TryCombine(int add)
	{
		int num = add + count;
		if (num > overlay)
		{
			count = overlay;
			return num - overlay;
		}
		count = num;
		return 0;
	}

	public Item TryCombine(Item item)
	{
		if (item == null)
		{
			return null;
		}
		if (IsSame(item))
		{
			int num = TryCombine(item.count);
			if (num > 0)
			{
				return item.Clone(num);
			}
			return null;
		}
		return item.Clone();
	}

	public virtual Item Clone(int count)
	{
		return ItemFactory.GenerateItem(proto, count);
	}

	public Item Clone()
	{
		return Clone(count);
	}

	public override string ToString()
	{
		return proto.ToString();
	}

	public virtual string GetDetailInfo()
	{
		return string.Join("\n", GetExtraInfo1() ?? "", GetExtraInfo2() ?? "").Trim();
	}

	public virtual string GetExtraInfo1()
	{
		return string.Empty;
	}

	public virtual string GetExtraInfo2()
	{
		if (proto.ElectricEnergy > 0)
		{
			return DolocConfig.StaticTexts.UiItemGenerateElectricityEntry;
		}
		return string.Empty;
	}

	public virtual Item CheckValid()
	{
		ItemFactory.ValidateItem(this, out var validItem);
		return validItem;
	}

	public void EmitSelf()
	{
		LinearInventory inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
		int num = inventory?.IndexOf(this) ?? (-1);
		if (num >= 0)
		{
			inventory?.ReEmit(num);
		}
	}

	public Item CostSelfFromInventory(LinearInventory inventory, bool showFadeUpIcon)
	{
		int num = inventory?.IndexOf(this) ?? (-1);
		if (num < 0)
		{
			return null;
		}
		if (showFadeUpIcon)
		{
			DolocAPI.RaiseSpriteFadeUp(DolocAPI.agent.PositionCenter, uiSprite);
		}
		if (count > 1)
		{
			count--;
			inventory?.ReEmit(num);
			return Clone(1);
		}
		QuickDeselect();
		return inventory?.Take(num);
	}

	public bool CostSelfFromInventory(LinearInventory inventory, out Item item, bool showFadeUpIcon, bool isQuickInventory = true)
	{
		item = null;
		int num = inventory?.IndexOf(this) ?? (-1);
		if (num < 0)
		{
			return false;
		}
		if (showFadeUpIcon)
		{
			DolocAPI.RaiseSpriteFadeUp(DolocAPI.agent.PositionCenter, uiSprite);
		}
		if (count > 1)
		{
			count--;
			inventory?.ReEmit(num);
			item = Clone(1);
			return true;
		}
		if (isQuickInventory)
		{
			QuickDeselect();
		}
		item = inventory?.Take(num);
		return true;
	}

	public Item CostSelf(bool showFadeUpIcon = true)
	{
		LinearInventory inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
		return CostSelfFromInventory(inventory, showFadeUpIcon);
	}

	public bool CostSelf(out Item item, bool showFadeUpIcon = true)
	{
		LinearInventory inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
		return CostSelfFromInventory(inventory, out item, showFadeUpIcon);
	}
}
