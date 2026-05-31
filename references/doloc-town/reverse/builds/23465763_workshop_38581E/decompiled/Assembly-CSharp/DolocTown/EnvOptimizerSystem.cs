using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.EnvOptimizer;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class EnvOptimizerSystem
{
	[CommandProperty("env_optimizer")]
	private static EnvOptimizerSystem _instance => DolocAPI.archiveHandle.farmData.envOptimizerSystem;

	[JsonProperty]
	public EnvOptimizerComponentSlot[] slots { get; private set; }

	public EnvOptimizerDataManager data { get; private set; } = new EnvOptimizerDataManager();


	public int slotCount => Enum.GetValues(typeof(EnvOptimizerComponentType)).Length;

	public Item[] currentSlotItems => slots.Select((EnvOptimizerComponentSlot x) => x.CurrentItem).ToArray();

	public int[] componentRequirePowerLevels { get; private set; }

	public int TotalPower => data.TotalCount;

	public int TotalLimitation => data.TotalLimitation;

	public float TotalProcess => data.TotalProcess;

	public Dictionary<EnvOptimizerBranchType, Vector2Int> PowerOfEachBranch => data.branches.ToDictionary((KeyValuePair<EnvOptimizerBranchType, EnvOptimizerBranch> x) => x.Key, (KeyValuePair<EnvOptimizerBranchType, EnvOptimizerBranch> x) => x.Value.GetPowerInfo());

	public string PowerStatus
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<EnvOptimizerBranchType, EnvOptimizerBranch> branch in data.branches)
			{
				stringBuilder.AppendLine($"\"{branch.Key}\"({branch.Value.Count}/{branch.Value.Limitation})");
			}
			return stringBuilder.ToString();
		}
	}

	public string SlotStatus
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			EnvOptimizerComponentSlot[] array = slots;
			foreach (EnvOptimizerComponentSlot envOptimizerComponentSlot in array)
			{
				stringBuilder.AppendLine((!envOptimizerComponentSlot.IsEmpty) ? ("- " + envOptimizerComponentSlot.itemName) : "- ");
			}
			return stringBuilder.ToString();
		}
	}

	public Dictionary<EnvOptimizerBranchType, EnvOptimizerBranch> Branches
	{
		get
		{
			Dictionary<EnvOptimizerBranchType, EnvOptimizerBranch> dictionary = new Dictionary<EnvOptimizerBranchType, EnvOptimizerBranch>();
			foreach (KeyValuePair<EnvOptimizerBranchType, EnvOptimizerBranch> branch in data.branches)
			{
				dictionary.Add(branch.Key, branch.Value.CloneBranch());
			}
			return dictionary;
		}
	}

	private TbEnvOptimizerSlot configTable => DolocConfig.Tables.TbEnvOptimizerSlot;

	[JsonConstructor]
	public EnvOptimizerSystem(EnvOptimizerComponentSlot[] slots = null)
	{
		if (slots == null)
		{
			slots = new EnvOptimizerComponentSlot[slotCount];
		}
		if (slots.Length != slotCount)
		{
			this.slots = new EnvOptimizerComponentSlot[slotCount];
			for (int i = 0; i < slotCount; i++)
			{
				this.slots[i] = ((i < slots.Length) ? slots[i] : new EnvOptimizerComponentSlot());
			}
		}
		else
		{
			this.slots = slots;
		}
		for (int j = 0; j < slotCount; j++)
		{
			EnvOptimizerComponentSlot[] array = this.slots;
			int num = j;
			if (array[num] == null)
			{
				array[num] = new EnvOptimizerComponentSlot();
			}
		}
		componentRequirePowerLevels = configTable.DataList.Select((EnvOptimizerSlotInfo x) => x.RequirePower).ToArray();
	}

	public void AfterLoadData()
	{
		ResetEnvOptimizerPoint();
	}

	public void ResetEnvOptimizerPoint()
	{
		int unlockedSeedCount = DolocAPI.archiveHandle.GetUnlockedSeedCount();
		for (int i = 0; i < unlockedSeedCount; i++)
		{
			data.PerformBehaviour(EnvOptimizerBehaviourType.UNLOCK_SEED, sendMessage: false);
		}
		int unlockedPlantDocumentCount = DolocAPI.archiveHandle.GetUnlockedPlantDocumentCount();
		for (int j = 0; j < unlockedPlantDocumentCount; j++)
		{
			data.PerformBehaviour(EnvOptimizerBehaviourType.UNLOCK_PLANT_DOC, sendMessage: false);
		}
		int fishDocumentCount = DolocAPI.archiveHandle.farmData.collectionManager.GetFishDocumentCount();
		for (int k = 0; k < fishDocumentCount; k++)
		{
			data.PerformBehaviour(EnvOptimizerBehaviourType.UNLOCK_FISH, sendMessage: false);
		}
		int animalDocumentCount = DolocAPI.archiveHandle.farmData.collectionManager.GetAnimalDocumentCount();
		for (int l = 0; l < animalDocumentCount; l++)
		{
			data.PerformBehaviour(EnvOptimizerBehaviourType.UNLOCK_ANIMAL, sendMessage: false);
		}
		int monsterDocumentCount = DolocAPI.archiveHandle.farmData.collectionManager.GetMonsterDocumentCount();
		for (int m = 0; m < monsterDocumentCount; m++)
		{
			data.PerformBehaviour(EnvOptimizerBehaviourType.UNLOCK_MONSTER, sendMessage: false);
		}
		foreach (CustomizedDocumentNodeInfo resourceDocument in DolocAPI.archiveHandle.farmData.collectionManager.GetResourceDocumentList())
		{
			data.PerformBehaviour(EnvOptimizerBehaviourType.UNLOCK_RESOURCE, resourceDocument, sendMessage: false);
		}
	}

	public int GetAvailableSlotCount()
	{
		int num = 0;
		int totalCount = data.TotalCount;
		int[] array = componentRequirePowerLevels;
		foreach (int num2 in array)
		{
			if (totalCount < num2)
			{
				break;
			}
			num++;
		}
		return num;
	}

	public EnvOptimizerComponentSlot[] GetAvailableSlots()
	{
		int availableSlotCount = GetAvailableSlotCount();
		if (availableSlotCount >= slotCount)
		{
			return slots;
		}
		return slots.Take(availableSlotCount).ToArray();
	}

	public int GetActiveSlotCount()
	{
		return slots.Count((EnvOptimizerComponentSlot x) => x.IsActive);
	}

	public bool IfItemIsComponent(string itemName)
	{
		return configTable.GetById(itemName ?? "") != null;
	}

	public bool IfItemIsComponent(Item item)
	{
		if (item == null)
		{
			return false;
		}
		return configTable.GetById(item.name ?? "") != null;
	}

	public bool IsComponentActive(EnvOptimizerComponentType type)
	{
		EnvOptimizerSlotInfo info = configTable.GetByComponentType(type);
		if (info == null)
		{
			return false;
		}
		return slots.Any((EnvOptimizerComponentSlot x) => x.IsActive && x.itemName == info.Id);
	}

	public string GetRequireComponent(EnvOptimizerComponentType type)
	{
		return configTable.GetByComponentType(type)?.Id;
	}

	public bool IsAllActive()
	{
		return slots.All((EnvOptimizerComponentSlot x) => x.IsActive);
	}

	public bool CanActive(out bool isEnergyEnough, out bool isComponentEnough)
	{
		EnvOptimizerComponentSlot[] availableSlots = GetAvailableSlots();
		int activeSlotCount = GetActiveSlotCount();
		isEnergyEnough = activeSlotCount < availableSlots.Length;
		isComponentEnough = activeSlotCount < slots.Length && !slots[activeSlotCount].IsEmpty;
		return availableSlots.Any((EnvOptimizerComponentSlot availableSlot) => !availableSlot.IsEmpty && !availableSlot.IsActive);
	}

	public bool TryActiveNext(out int slotIndex, out EnvOptimizerComponentSlot slot)
	{
		slotIndex = -1;
		slot = null;
		int availableSlotCount = GetAvailableSlotCount();
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		for (int i = 0; i < availableSlotCount; i++)
		{
			EnvOptimizerComponentSlot envOptimizerComponentSlot = slots[i];
			if (!envOptimizerComponentSlot.IsActive && envOptimizerComponentSlot.SetActive(dateNow))
			{
				slotIndex = i;
				slot = envOptimizerComponentSlot;
				return true;
			}
		}
		return false;
	}

	public bool TryPlaceItemAtIndex(int index, Item item)
	{
		if (!CheckSlotIndexValid(index))
		{
			return false;
		}
		EnvOptimizerComponentSlot envOptimizerComponentSlot = slots[index];
		if (item == null || !IfItemIsComponent(item))
		{
			return false;
		}
		return envOptimizerComponentSlot.PlaceItem(item);
	}

	public bool CheckSlotIndexValid(int index)
	{
		if (index < 0 || index >= slots.Length)
		{
			return false;
		}
		return true;
	}

	public void Debug_ForceAddPoint(EnvOptimizerBranchType type, int point)
	{
		data.Debug_ForceAddPoint(type, point);
	}

	public void Debug_ForceAddPointForAllBranches(int point)
	{
		data.Debug_ForceAddPointForAllBranches(point);
	}
}
