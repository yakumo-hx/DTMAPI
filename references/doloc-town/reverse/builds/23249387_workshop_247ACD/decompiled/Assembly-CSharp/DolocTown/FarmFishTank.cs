using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class FarmFishTank
{
	private List<string> _products = new List<string>();

	private readonly List<ItemFishFry> fries = new List<ItemFishFry>();

	public Equipment fishTankEquipment { get; private set; }

	private EquipmentInfo equipmentProto => fishTankEquipment.proto;

	private EquipmentFuncFishTankBase fishTankFunc => equipmentProto.Function as EquipmentFuncFishTankBase;

	public bool Valid
	{
		get
		{
			if (fishTankEquipment != null && equipmentProto != null)
			{
				return fishTankFunc != null;
			}
			return false;
		}
	}

	public bool IsDirty
	{
		get
		{
			if (inventory.isEmpty)
			{
				return _products.Count > 0;
			}
			return true;
		}
	}

	[JsonProperty]
	public string name => fishTankEquipment.Name;

	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	[JsonProperty]
	public LinearInventory extensionInventory { get; private set; }

	[JsonProperty]
	[DebugInfo("当前代谢值")]
	public int metabolism { get; private set; }

	[JsonProperty]
	public string[] products => _products.ToArray();

	public bool IsProductFull => _products.Count >= fishTankFunc.ProductCapacity;

	public bool HasProduct => _products.Count > 0;

	public int TotalFishCount => fishShoal.TotalFishCount + fries.Count;

	public FarmFishShoal fishShoal { get; private set; } = new FarmFishShoal();


	[DebugInfo("代谢值扩展增量")]
	public int ExtensionMetabolismIncrease { get; private set; }

	[DebugInfo("饲料槽扩展容量")]
	public int ExtensionEnergyCapacity { get; private set; }

	public int TotalMetabolismIncrease => fishShoal.TotalMetabolismIncrease + ExtensionMetabolismIncrease;

	public FarmFishTank(Equipment fishTankEquipment)
	{
		this.fishTankEquipment = fishTankEquipment;
		inventory = new LinearInventory(fishTankFunc.TotalCapacity);
		extensionInventory = new LinearInventory(equipmentProto.ContainedSlots.Length);
	}

	[JsonConstructor]
	protected FarmFishTank(string name, LinearInventory inventory, LinearInventory extensionInventory, int metabolism, string[] products)
	{
		this.inventory = inventory;
		this.extensionInventory = extensionInventory;
		this.metabolism = metabolism;
		_products = new List<string>(products ?? Array.Empty<string>());
	}

	public void SetEquipment(Equipment fishTank)
	{
		fishTankEquipment = fishTank;
		if (inventory == null)
		{
			LinearInventory linearInventory2 = (inventory = new LinearInventory(fishTankFunc.TotalCapacity));
		}
		inventory.ValidateCapacity(fishTankFunc.TotalCapacity);
		ResolveFishTank();
	}

	public void OnHostRemove(bool putInBackpack)
	{
		foreach (Item productItem in GetProductItems())
		{
			fishTankEquipment.PlaceItemInBagOrCreateDropItem(productItem, putInBackpack, sendMessage: true);
		}
		extensionInventory.Clear();
	}

	private void RefreshFishTankExtensions()
	{
		ExtensionMetabolismIncrease = 0;
		ExtensionEnergyCapacity = 0;
		Item[] array = extensionInventory.ReadAllWithNull();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemEquipment itemEquipment && itemEquipment.EquipmentProto.Function is EquipmentFuncFishTankExtension equipmentFuncFishTankExtension)
			{
				ExtensionMetabolismIncrease += equipmentFuncFishTankExtension.MetabolismIncrease;
				ExtensionEnergyCapacity += equipmentFuncFishTankExtension.EnergyIncrease;
			}
		}
	}

	public int ResolveFishTank()
	{
		ApplyFishTankExtensions();
		fishShoal.Clear();
		fries.Clear();
		Item[] array = inventory.ReadAll();
		foreach (Item item in array)
		{
			if (item is ItemFishFry item2)
			{
				fries.Add(item2);
			}
			else
			{
				fishShoal.AddFish(item.name);
			}
		}
		fishShoal.UpdateStatus();
		return fishShoal.TotalFishCount + fries.Count;
	}

	private FishTankExtension[] GetFishTankExtensions()
	{
		return fishTankEquipment.GetDecalsOfType<FishTankExtension>();
	}

	public void RefreshFishTankExtensionsInventory()
	{
		extensionInventory = new LinearInventory(fishTankEquipment.proto.ContainedSlots.Length);
		foreach (var (index, decal2) in fishTankEquipment.AttachedDecals)
		{
			if (decal2 is FishTankExtension fishTankExtension)
			{
				extensionInventory.PlaceItemAt(index, new ItemEquipment(fishTankExtension.Name, 1));
			}
		}
	}

	public void ApplyFishTankExtensions()
	{
		if (!DolocAPI.IsDataLoaded || fishTankEquipment.Host == null)
		{
			RefreshFishTankExtensions();
			return;
		}
		Item[] array = extensionInventory.ReadAllWithNull();
		RemoveAllFishTankExtensions();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemEquipment itemEquipment && itemEquipment.EquipmentProto.Function is EquipmentFuncFishTankExtension)
			{
				fishTankEquipment.Host.CreateEquipment(Vector3.zero, Vector2Int.zero, itemEquipment.EquipmentProto, turn: false, fishTankEquipment, i).RefreshPosition();
			}
		}
		RefreshFishTankExtensions();
	}

	public void RemoveAllFishTankExtensions()
	{
		FishTankExtension[] fishTankExtensions = GetFishTankExtensions();
		foreach (FishTankExtension equipment in fishTankExtensions)
		{
			fishTankEquipment.Host.RemoveEquipment(equipment, putInBackpack: false, retrieveItem: false);
		}
	}

	public void GenFishProcut()
	{
		FillProduct(GetFormationProduct());
	}

	public void SimulateFishProduct()
	{
		CountItem[] formationProduct = GetFormationProduct();
		for (int i = 0; i < formationProduct.Length; i++)
		{
			CountItem countItem = formationProduct[i];
			Debug.Log($"<color=yellow>鱼缸测试产出:{countItem.itemName}({countItem.itemCount})</color>");
		}
	}

	public int AddNaturalMetabolism(int energy)
	{
		if (energy <= 0)
		{
			return 0;
		}
		int num = Mathf.Min(energy, fishShoal.TotalEnergyCost);
		AddMetabolism(fishShoal.GetMetabolism(num) + ExtensionMetabolismIncrease);
		energy -= num;
		return energy;
	}

	public void AddMetabolism(int value)
	{
		if (value > 0)
		{
			metabolism += value;
			if (metabolism >= fishTankFunc.MetabolismThreshold)
			{
				metabolism -= fishTankFunc.MetabolismThreshold;
				GenFishProcut();
			}
		}
	}

	public void GrowFries()
	{
		if (fries.Count == 0)
		{
			return;
		}
		Queue<ItemFishFry> queue = new Queue<ItemFishFry>();
		foreach (ItemFishFry item in fries.Where((ItemFishFry fry) => fry.Grow()))
		{
			queue.Enqueue(item);
		}
		while (queue.Count > 0)
		{
			ItemFishFry itemFishFry = queue.Dequeue();
			fries.Remove(itemFishFry);
			int num = inventory.IndexOf(itemFishFry);
			if (num >= 0)
			{
				inventory.SwapItem(num, itemFishFry.ToFishItem());
				DolocAPI.BroadcastString(GameEventType.FRY_GROW_UP, itemFishFry.fishName);
				DolocAPI.archiveHandle.RecordCollection(CollectionType.Fish, itemFishFry.fishName);
			}
		}
	}

	private void FillProduct(CountItem[] output)
	{
		if (output.Length == 0)
		{
			return;
		}
		for (int i = 0; i < output.Length; i++)
		{
			CountItem countItem = output[i];
			if (IsProductFull)
			{
				break;
			}
			for (int j = 0; j < countItem.itemCount; j++)
			{
				TriggerEvent(countItem.itemName);
				_products.Add(countItem.itemName);
				if (IsProductFull)
				{
					return;
				}
			}
		}
	}

	private void TriggerEvent(string outputItemName)
	{
		if (!outputItemName.IsNullOrEmpty() && DolocConfig.Tables.TbFarmFish.DataMap.ContainsKey(outputItemName))
		{
			DolocAPI.BroadcastString(GameEventType.AQUA_FISH_BIRTH, outputItemName);
		}
	}

	private static CountItem[] _GenFarmFishProduct(FarmFishInfo proto)
	{
		ItemSpawnEntry produceSpawnEntry = proto.ProduceSpawnEntry;
		if (produceSpawnEntry.SpawnLut_Ref == null)
		{
			return Array.Empty<CountItem>();
		}
		return produceSpawnEntry.SpawnLut_Ref.SpawnItems(produceSpawnEntry.CountRange.MinCount, produceSpawnEntry.CountRange.MaxCount);
	}

	private CountItem[] GetDefaultProduct()
	{
		FarmFishInfo farmFishInfo = fishShoal.RollFish();
		if (farmFishInfo == null)
		{
			return Array.Empty<CountItem>();
		}
		return _GenFarmFishProduct(farmFishInfo);
	}

	private CountItem[] GetFormationProduct()
	{
		FarmFishFormationInfo[] allSatisfiedFormations = FarmFishFormationInfo.GetAllSatisfiedFormations(from x in inventory.ReadAll()
			select x.name);
		if (allSatisfiedFormations.Length == 0)
		{
			return GetDefaultProduct();
		}
		List<int> list = new List<int>();
		list.Add(DolocAPI.GlobalParameter.AquaDefaultWeight);
		list.AddRange(allSatisfiedFormations.Select((FarmFishFormationInfo x) => x.Weight));
		int num = RandomUtils._RussianRoulette(list.ToArray());
		if (num == 0)
		{
			return GetDefaultProduct();
		}
		FarmFishFormationInfo farmFishFormationInfo = allSatisfiedFormations[num - 1];
		return DolocAPI.SpawnItems(farmFishFormationInfo.FormationOutputFish, farmFishFormationInfo.CountRange);
	}

	public void CollectProductItems()
	{
		foreach (Item productItem in GetProductItems())
		{
			fishTankEquipment.CreateDropItem(productItem, shouldRender: true, sendMessage: true);
		}
	}

	public IEnumerable<Item> GetProductItems()
	{
		if (_products.Count == 0)
		{
			yield break;
		}
		foreach (string product in _products)
		{
			if (DolocConfig.Tables.TbFarmFish.IsFarmFish(product, out var proto))
			{
				Item item = DolocAPI.GenerateItem(proto.RoeItem_Ref);
				if (item is ItemFishRoe itemFishRoe)
				{
					itemFishRoe.SetFishName(product);
				}
				yield return item;
			}
			else
			{
				yield return DolocAPI.GenerateItem(product);
			}
		}
		DolocAPI.AddTechExp(TechPointType.ANIMAL, DolocAPI.GlobalParameter.AquaTechpointProduce);
		_products.Clear();
	}

	public void ShuffleFishes()
	{
		List<(int, Item)> list = new List<(int, Item)>();
		int num = -1;
		Item[] array = inventory.ReadAllWithNull();
		foreach (Item item in array)
		{
			num++;
			if (!(item is ItemFishRoe))
			{
				list.Add((num, item));
			}
		}
		foreach (var item4 in list)
		{
			int item2 = item4.Item1;
			inventory.Take(item2);
		}
		inventory.Shuffle();
		List<Item> list2 = new List<Item>();
		foreach (var (index, item3) in list)
		{
			if (inventory.Read(index) != null)
			{
				list2.Add(inventory.Take(index));
			}
			inventory.PlaceItemAt(index, item3);
		}
		foreach (Item item5 in list2)
		{
			inventory.PlaceItemAt(inventory.FirstEmptyIndex, item5);
		}
	}

	public bool ExtensionItemFilter(Item item)
	{
		if (item is ItemEquipment itemEquipment)
		{
			return itemEquipment.EquipmentProto.Function is EquipmentFuncFishTankExtension;
		}
		return false;
	}

	public bool ContentFilter(Item content)
	{
		if (content == null)
		{
			return false;
		}
		if (!DolocConfig.Tables.TbFarmFish.IsFarmFish(content.name, out var _))
		{
			return content is ItemFishFry;
		}
		return true;
	}

	public void OpenFishTankUI(Action onUiExit)
	{
		DolocAPI.EnterUI((FishTankUiState state) => state.HandleContainerStartUpArgs(fishTankEquipment as IContainer, DolocConfig.StaticTexts.InventoryPanelSocketTitle, extensionInventory, ExtensionItemFilter, onUiExit, GetInfoText));
	}

	private string GetInfoText()
	{
		int num = 0;
		Item[] array = extensionInventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemEquipment itemEquipment && itemEquipment.EquipmentProto.Function is EquipmentFuncFishTankExtension equipmentFuncFishTankExtension)
			{
				num += equipmentFuncFishTankExtension.EnergyIncrease;
			}
		}
		if (!(fishTankEquipment is IFishTank { EnergyCapacity: >0 } fishTank))
		{
			return string.Empty;
		}
		return DolocUtils.Format(DolocConfig.StaticTexts.FishTankPanelFeedQuantity, $"{fishTank.Energy}/{fishTank.EnergyCapacity + num}");
	}
}
