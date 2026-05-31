using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class GeneSynthesizer : EquipmentWorker, IContainer
{
	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	private EquipmentFuncGeneSynthesizer func => proto.Function as EquipmentFuncGeneSynthesizer;

	public string title => proto.Title;

	public int totalCapacity => 2;

	public int lineCapacity => 2;

	public GeneSynthesizer(IEquipmentHost room, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(room, instanceId, proto, worldPos, anchor, turn)
	{
		inventory = new LinearInventory(totalCapacity);
	}

	[JsonConstructor]
	public GeneSynthesizer(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter, LinearInventory inventory)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter)
	{
		this.inventory = inventory;
		inventory.ValidateCapacity(totalCapacity);
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
		DolocAPI.EnterUI((FishTankUiState state) => state.HandleContainerStartUpArgs(this, OnUiExit, () => DolocConfig.StaticTexts.InventoryPanelInfoGeneSynthesizer));
	}

	private void OnUiExit()
	{
		TryStartWork();
	}

	private void TryStartWork()
	{
		if (inventory.filledCount >= 2)
		{
			if (base.IsRender)
			{
				Work(func.Interval);
			}
			else
			{
				WorkNoRender(func.Interval);
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
		Item[] array = inventory.ReadAll();
		if (array.Length >= 2 && array[0] is IHasGeneGroup hasGeneGroup && array[1] is IHasGeneGroup hasGeneGroup2)
		{
			GeneGroup geneGroup = hasGeneGroup.GeneGroup.Synthesize(hasGeneGroup2.GeneGroup);
			Item[] array2 = array.Where((Item x) => x is ItemSeed).ToArray();
			Item item = ((array2.Length == 0) ? DolocAPI.GenerateItem(func.DefaultCapsuleItem) : array2.Choice().Clone(1));
			((IHasGeneGroup)item)?.SetGeneGroup(geneGroup);
			this.CreateDropItem(item?.CheckValid(), isRender, sendMessage: false);
			inventory.Clear();
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
		if (content is ItemGeneCapsule)
		{
			return true;
		}
		if (content is ItemSeed { IsCloned: false })
		{
			if (func.AllowCrossSpecies)
			{
				return true;
			}
			Item[] array = (from x in inventory.ReadAll()
				where x is ItemSeed
				select x).ToArray();
			if (inventory.isEmpty || array.Length == 0 || array[0].name == content.name)
			{
				return true;
			}
		}
		return false;
	}
}
