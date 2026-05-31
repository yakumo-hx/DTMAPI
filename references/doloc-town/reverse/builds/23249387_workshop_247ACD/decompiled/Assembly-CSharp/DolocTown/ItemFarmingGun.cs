using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemFarmingGun : Item, IContainer, IActiveItem
{
	private readonly ItemFunctionFarmingGun func;

	public override string title
	{
		get
		{
			if (!inventory.isEmpty)
			{
				return DolocUtils.Format(DolocConfig.StaticTexts.ItemTitleInfoFormat, base.title, inventory.FirstItem.title);
			}
			return base.title;
		}
	}

	string IContainer.title => base.title;

	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	public int totalCapacity => func.Capacity;

	public int lineCapacity => func.Capacity;

	public ItemFarmingGun(ItemInfo proto, int count)
		: base(proto, count)
	{
		func = (ItemFunctionFarmingGun)proto.Function;
		inventory = new LinearInventory(func.Capacity);
	}

	[JsonConstructor]
	protected ItemFarmingGun(string itemName, int itemCount, LinearInventory inventory)
		: base(itemName, itemCount)
	{
		if (base.proto != null)
		{
			func = (ItemFunctionFarmingGun)base.proto.Function;
			this.inventory = inventory ?? new LinearInventory(func.Capacity);
		}
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		InitCellTip();
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	private void InitCellTip()
	{
		if (base.isQuickSelected && !inventory.isEmpty && DolocAPI.IsPlayerInFarmScene)
		{
			ShowCellTip(func.Offset, func.Area, flipWhenFaceLeft: true);
			RefreshCellTip();
		}
		else
		{
			HideCellTip();
		}
	}

	protected override void RefreshCellTip()
	{
		base.cellTip.CellTipValid = CheckCanInteract(GetEquipmentsFromArea(), inventory.FirstItem);
	}

	private void OpenContainerUI()
	{
		DolocAPI.EnterUI((FarmingGunUiState state) => state.HandleStartUpArgs(this, delegate
		{
			InitCellTip();
			EmitSelf();
		}));
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		OpenContainerUI();
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		if (inventory.isEmpty)
		{
			OpenContainerUI();
			return;
		}
		Item item = inventory.FirstItem.Clone(1);
		Equipment[] equipments = GetEquipmentsFromArea();
		if (!CheckCanInteract(equipments, item))
		{
			if (item is ItemSeed itemSeed && !itemSeed.CheckCurrentSeasonValid())
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrInvalidSeason, item.title));
			}
			else
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrNoPlantBasin);
			}
			return;
		}
		Sprite icon = item.uiSprite;
		Vector3 from = DolocAPI.AgentPosition;
		float duration = 0.35f;
		HideCellTip();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_USE_FARMING_GUN);
		DolocAPI.agent._Interact(delegate
		{
			bool flag = false;
			Equipment[] array = equipments;
			foreach (Equipment equipment in array)
			{
				if (inventory.isEmpty)
				{
					return;
				}
				if (CheckCanInteract(equipment, item))
				{
					inventory.TryCost(item.name, 1);
					DolocAPI.MoveFadeOutSprite(from, equipment.PositionCenter, icon, duration);
					flag = true;
					DolocAPI.Delay(duration, delegate
					{
						DoInteract(equipment, item);
					});
				}
			}
			if (flag)
			{
				ShineArea();
			}
			DolocAPI.Delay(duration, InitCellTip);
		}, "chop");
	}

	private Equipment[] GetEquipmentsFromArea()
	{
		return ((IBaseHost)DolocAPI.CurrentRoom).DM_terrain.GetContentsFromArea<Equipment>(base.cellTip.CellAnchor, base.cellTip.CellSize).ToArray();
	}

	private bool CheckCanInteract(Equipment[] basins, Item item)
	{
		if (item == null || basins.IsNullOrEmpty())
		{
			return false;
		}
		foreach (Equipment equipment in basins)
		{
			if (CheckCanInteract(equipment, item))
			{
				return true;
			}
		}
		return false;
	}

	private bool CheckCanInteract(Equipment equipment, Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (equipment is PlantBasin plantBasin)
		{
			if (item is ItemFilm)
			{
				return !plantBasin.IsProtected;
			}
			if (item is ItemSeed itemSeed)
			{
				if (!plantBasin.IsPlanted && itemSeed.seedProto.SeedType == plantBasin.SeedTypeInfo.Id)
				{
					return itemSeed.CheckCurrentSeasonValid();
				}
				return false;
			}
			if (item is ItemFertilizer fertilizer)
			{
				return plantBasin.CanFertilizer(fertilizer);
			}
			if (item.name == DolocAPI.GlobalParameter.ItemRefBottleOfWater)
			{
				return plantBasin.Supply.WaterRatio <= 0.6f;
			}
		}
		if (equipment is PlantBasinTree plantBasinTree && item is ItemFertilizer fertilizer2)
		{
			return plantBasinTree.CanFertilizer(fertilizer2);
		}
		return false;
	}

	private bool DoInteract(Equipment equipment, Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (equipment is PlantBasin plantBasin)
		{
			if (item is ItemFilm itemFilm)
			{
				return plantBasin.Protect(itemFilm, isRender: true);
			}
			if (item is ItemSeed seed)
			{
				return plantBasin.Plant(seed, shouldRender: true, sendMessage: true);
			}
			if (item is ItemFertilizer fertilizer)
			{
				return plantBasin.Fertilizer(fertilizer, shouldRender: true, sendMessage: true);
			}
			if (item.name == DolocAPI.GlobalParameter.ItemRefBottleOfWater)
			{
				if (plantBasin.Supply.WaterRatio > 0.6f)
				{
					return false;
				}
				plantBasin.Water(shouldRender: true);
				DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, DolocAPI.GlobalParameter.ItemRefWastePlasticBottle, DolocAPI.AgentPosition);
				return true;
			}
		}
		if (equipment is PlantBasinTree plantBasinTree && item is ItemFertilizer fertilizer2)
		{
			return plantBasinTree.Fertilizer(fertilizer2, shouldRender: true, shouldSendMessage: true);
		}
		return false;
	}

	protected void ShineArea()
	{
		GridArea areaRenderer = DolocAPI.EntitySystem.Next<GridArea>();
		if (areaRenderer == null)
		{
			return;
		}
		areaRenderer.GridSize = base.cellTip.CellSize;
		areaRenderer.GridPosition = BuilderUtils.GetRealGridPos(base.cellTip.CellAnchor, DolocAPI.CurrentRoom.RoomPosition);
		areaRenderer.ShineArea(Color.white, delegate
		{
			DolocAPI.DelayFrame(delegate
			{
				DolocAPI.EntitySystem.Recycle(areaRenderer);
			});
		}, 1f, 0.3f, 0.3f, 1f);
	}

	public override Item Clone(int count)
	{
		return new ItemFarmingGun(name, count, inventory);
	}

	protected override bool CanPutInToContainer()
	{
		return true;
	}

	public bool ContentFilter(Item content)
	{
		if (content == null)
		{
			return false;
		}
		if (content.name == DolocAPI.GlobalParameter.ItemRefBottleOfWater)
		{
			return true;
		}
		if (!(content is ItemFilm) && !(content is ItemSeed))
		{
			return content is ItemFertilizer;
		}
		return true;
	}

	public void InvokeActiveItem()
	{
	}
}
