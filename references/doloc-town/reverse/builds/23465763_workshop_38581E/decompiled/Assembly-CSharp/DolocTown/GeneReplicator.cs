using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class GeneReplicator : EquipmentWorker, IContainer
{
	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	[JsonProperty]
	public LinearInventory templateInventory { get; private set; }

	private EquipmentFuncGeneReplicator func => proto.Function as EquipmentFuncGeneReplicator;

	public string title => proto.Title;

	public int totalCapacity => func.TotalCapacity;

	public int lineCapacity => func.LineCapacity;

	public GeneReplicator(IEquipmentHost room, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(room, instanceId, proto, worldPos, anchor, turn)
	{
		inventory = new LinearInventory(totalCapacity);
		templateInventory = new LinearInventory(1);
	}

	[JsonConstructor]
	public GeneReplicator(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter, LinearInventory inventory, LinearInventory templateInventory)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter)
	{
		this.inventory = inventory ?? new LinearInventory(totalCapacity);
		this.inventory.ValidateCapacity(totalCapacity);
		this.templateInventory = templateInventory ?? new LinearInventory(1);
		this.templateInventory.ValidateCapacity(1);
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
		DolocAPI.EnterUI((FishTankUiState state) => state.HandleContainerStartUpArgs(this, DolocConfig.StaticTexts.InventoryPanelCapsuleTitle, templateInventory, GeneTemplateFilter, OnUiExit, GetInfoText, CheckBufferLocked, refreshFilterOnInventoryChange: true));
	}

	private string GetInfoText()
	{
		if (!base.IsWorking)
		{
			return DolocConfig.StaticTexts.InventoryPanelInfoGeneReplicator;
		}
		string formatTimeLengthByTU = DolocAPI.GetFormatTimeLengthByTU(GetWorkInterval() - base.WorkCounter.Value / DolocAPI.GlobalParameter.TULength);
		return DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelRestTime, formatTimeLengthByTU.Colored(DolocUiColor.TEXTCOLOR_STD) ?? "");
	}

	private int GetWorkInterval()
	{
		if (templateInventory.FirstItem is IHasGeneGroup { HasUnnaturalGenes: false } hasGeneGroup && hasGeneGroup is ItemSeed)
		{
			return func.ClearInterval;
		}
		return func.CopyInterval;
	}

	private bool CheckBufferLocked(bool showMessage)
	{
		if (base.IsWorking && showMessage && !inventory.isEmpty)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.InventoryPanelGeneReplicatorLocked, inventory.FirstItem.title));
		}
		return base.IsWorking;
	}

	private void OnUiExit()
	{
		TryStartWork();
	}

	private void TryStartWork()
	{
		if (!base.IsWorking && !inventory.isEmpty && !templateInventory.isEmpty)
		{
			if (base.IsRender)
			{
				Work(GetWorkInterval());
			}
			else
			{
				WorkNoRender(GetWorkInterval());
			}
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
		if (inventory.isEmpty || templateInventory.isEmpty || !(templateInventory.FirstItem is IHasGeneGroup hasGeneGroup))
		{
			return;
		}
		Item[] array = inventory.ReadAll();
		foreach (Item item in array)
		{
			if (item is ItemSeed)
			{
				Item item2 = item.Clone(1);
				((ItemSeed)item2).CloneGeneGroup(hasGeneGroup.GeneGroup);
				this.CreateDropItem(item2, isRender, sendMessage: false);
			}
		}
		inventory.Clear();
		templateInventory.Clear();
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
		linearInventory = templateInventory;
		if (linearInventory != null && !linearInventory.isEmpty)
		{
			Item[] array = templateInventory.ReadAll();
			foreach (Item item2 in array)
			{
				this.PlaceItemInBagOrCreateDropItem(item2, putInBackpack, sendMessage: false);
			}
			templateInventory.Clear();
		}
	}

	public bool ContentFilter(Item content)
	{
		if (!(content is ItemSeed { IsCloned: false } itemSeed))
		{
			return false;
		}
		if (templateInventory.FirstItem is ItemSeed itemSeed2 && itemSeed2.name != itemSeed.name)
		{
			return false;
		}
		return true;
	}

	public bool GeneTemplateFilter(Item content)
	{
		if (!templateInventory.isEmpty)
		{
			return false;
		}
		if (content is ItemGeneCapsule)
		{
			return true;
		}
		if (content is ItemSeed itemSeed)
		{
			if (itemSeed.IsCloned)
			{
				return false;
			}
			Item[] array = inventory.ReadAll();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].name != content.name)
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}
}
