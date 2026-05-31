using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class GeneIncubator : EquipmentWorker, IContainer
{
	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	private EquipmentFuncGeneIncubator func => proto.Function as EquipmentFuncGeneIncubator;

	public string title => proto.Title;

	public int totalCapacity => func.TotalCapacity;

	public int lineCapacity => func.LineCapacity;

	public GeneIncubator(IEquipmentHost room, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(room, instanceId, proto, worldPos, anchor, turn)
	{
		inventory = new LinearInventory(func.TotalCapacity);
	}

	[JsonConstructor]
	public GeneIncubator(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter, LinearInventory inventory)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter)
	{
		this.inventory = inventory;
		inventory.ValidateCapacity(func.TotalCapacity);
	}

	protected override void OnTouch()
	{
		if (!base.IsWorking)
		{
			ShowTip(DolocConfig.StaticTexts.UiOperationInteract);
		}
	}

	protected override void OnDisTouch()
	{
		HideTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		PushTipToHide();
		ClearIncubationProgressInBackpack();
		DolocAPI.EnterUI((FishTankUiState state) => state.HandleContainerStartUpArgs(this, OnUiExit, () => DolocConfig.StaticTexts.InventoryPanelInfoGeneIncubator));
	}

	private void OnUiExit()
	{
		if (!inventory.isEmpty)
		{
			StartWork();
		}
		ClearIncubationProgressInBackpack();
		Item[] array = inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemSeed itemSeed)
			{
				itemSeed.ResetOverflowIncubationProgress(func.IncubationThreshold);
			}
		}
	}

	private void ClearIncubationProgressInBackpack()
	{
		Item[] array = DolocAPI.archiveHandle.InventorySystem.inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemSeed itemSeed)
			{
				itemSeed.ClearIncubationProgress(func.IncubationThreshold);
			}
		}
	}

	private void StartWork()
	{
		if (base.IsRender)
		{
			Work(1);
		}
		else
		{
			WorkNoRender(1);
		}
	}

	protected override void OnWorkDone()
	{
		OnWorkDoneInternal(isRender: true);
	}

	protected override void OnWorkDoneNoRender()
	{
		OnWorkDoneInternal(isRender: false);
	}

	protected virtual void OnWorkDoneInternal(bool isRender)
	{
		if (inventory.isEmpty)
		{
			return;
		}
		bool flag = false;
		Item[] array = inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemSeed itemSeed)
			{
				flag |= itemSeed.Incubate(func.IncubationThreshold);
			}
		}
		if (flag)
		{
			StartWork();
		}
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		LinearInventory linearInventory = inventory;
		if (linearInventory != null && !linearInventory.isEmpty)
		{
			Item[] array = inventory.ReadAll();
			foreach (Item item in array)
			{
				this.PlaceItemInBagOrCreateDropItem(item, putInBackpack, sendMessage: false);
			}
			inventory.Clear();
		}
	}

	public bool ContentFilter(Item content)
	{
		if (content is ItemSeed itemSeed)
		{
			return !itemSeed.IsCloned;
		}
		return false;
	}
}
