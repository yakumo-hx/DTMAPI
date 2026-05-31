using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemWaterCan : Item, IDurability, IWaterContainer
{
	private readonly ItemFunctionWaterCan func;

	[JsonProperty]
	private int currentValue;

	private int capacity => func.Capacity;

	private int range => func.Range;

	private bool endlessWater => capacity == -1;

	private int CurrentValue
	{
		get
		{
			if (!endlessWater)
			{
				return currentValue;
			}
			return int.MaxValue;
		}
		set
		{
			if (endlessWater)
			{
				currentValue = capacity;
			}
			else
			{
				currentValue = Mathf.Max(value, 0);
			}
		}
	}

	public int maxDurability => capacity;

	public int currentDurability => currentValue;

	public bool IsFull => currentValue == capacity;

	public int lackDurability => capacity - currentValue;

	public Vector2Int CellSize => base.cellTip.CellSize;

	public Vector2 WorldCellPos => base.cellTip.WorldCellPos;

	int IWaterContainer.Water => CurrentValue;

	public ItemWaterCan(ItemInfo item, int count)
		: base(item, count)
	{
		func = (ItemFunctionWaterCan)base.proto.Function;
		CurrentValue = 0;
	}

	[JsonConstructor]
	protected ItemWaterCan(string itemName, int itemCount, int currentValue)
		: base(itemName, itemCount)
	{
		func = (ItemFunctionWaterCan)base.proto.Function;
		CurrentValue = currentValue;
	}

	public void SupplyToFull()
	{
		Supply(capacity);
	}

	public void Supply(int value)
	{
		CurrentValue = Mathf.Min(currentValue + value, capacity);
		DolocAPI.RefreshQuickInventorySelected();
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Use();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		if (!TryDrawWater())
		{
			if (!DolocAPI.HasEnoughEnergyForUsingTool())
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrLackOfEnergy);
			}
			else if (CurrentValue > 0)
			{
				CurrentValue--;
				DolocAPI.RefreshQuickInventorySelected();
				DolocAPI.gameStateManager.agentController.Water(this, Water);
				DolocAPI.Broadcast(OperationEventType.USE_TOOL);
			}
			else
			{
				DolocAPI.RaiseEmotion(DolocAPI.AgentTransform, EmotionName.CONFUSE);
				DolocAPI.ShowMessageBoxSmall(DolocConfig.StaticTexts.UiOperationErrRunoutWater);
			}
		}
	}

	private bool TryDrawWater()
	{
		if (IsFull)
		{
			return false;
		}
		Equipment selectedEquipmentSource = base.SelectedEquipmentSource;
		IWaterContainer container = selectedEquipmentSource as IWaterContainer;
		if (container != null)
		{
			DolocAPI.agent._Interact(delegate
			{
				int num = container.TakeWater(lackDurability);
				if (num > 0)
				{
					Supply(num);
				}
			});
			return true;
		}
		if (DolocAPI.IsAgentInWater)
		{
			DolocAPI.agent._Interact(SupplyToFull);
			return true;
		}
		return false;
	}

	protected void Water()
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom == null)
		{
			return;
		}
		HashSet<Equipment> equipmentSet = new HashSet<Equipment>();
		foreach (Vector2Int cellPosition in base.cellTip.CellPositions)
		{
			WaterToCell(currentRoom, cellPosition, equipmentSet);
		}
	}

	private void WaterToCell(Room room, Vector2Int cell, HashSet<Equipment> equipmentSet)
	{
		DolocAPI.RaiseInstantAnimEffects(room.Geometry.CalcWorldPosition(cell, new Vector2(0.5f, 0f)), InstAnimEffectType.WATER_LITTLE);
		Equipment content = room.DM_terrain.GetContent<Equipment>(cell, TerrainLayerName.Equipment);
		if (equipmentSet.Add(content))
		{
			((IWaterable)content)?.OnWater();
		}
	}

	int IWaterContainer.TakeWater(int require)
	{
		if (require <= 0 || CurrentValue <= 0)
		{
			return 0;
		}
		int num = Mathf.Min(CurrentValue, require);
		CurrentValue -= num;
		EmitSelf();
		return num;
	}

	void IWaterContainer.Evaporation(int value, bool shouldRender)
	{
	}

	public override Item Clone(int count)
	{
		return new ItemWaterCan(name, count, currentValue);
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		if (DolocAPI.IsPlayerInFarmScene)
		{
			ShowCellTip(new Vector2Int(-Mathf.FloorToInt((float)range * 0.5f), 0), new Vector2Int(range, 1), flipWhenFaceLeft: true);
		}
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	protected override void RefreshCellTip()
	{
		Equipment selectedEquipmentSource = base.SelectedEquipmentSource;
		base.cellTip.CellTipValid = selectedEquipmentSource is PlantBasin || selectedEquipmentSource is IWaterContainer;
	}

	public override string GetExtraInfo1()
	{
		string str = (endlessWater ? DolocConfig.StaticTexts.ItemWaterCanEndlessWater : func.Capacity.ToString());
		return DolocUtils.Format(DolocConfig.StaticTexts.ItemWaterCanArea, str.Colored(DolocUiColor.TEXTCOLOR_STD), func.Range.ToString().Colored(DolocUiColor.TEXTCOLOR_STD));
	}
}
