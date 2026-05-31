using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.EnvOptimizer;
using DolocTown.Config.Settings;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class PlantDocumentManager
{
	[JsonProperty]
	public List<string> unlockedOrder = new List<string>();

	public Dictionary<string, PlantDocumentInfo> unlockedDocuments = new Dictionary<string, PlantDocumentInfo>();

	private string latestPlantTitles;

	private int latestPlantRewards;

	private int latestPlantCount;

	[JsonProperty]
	private List<string> unanalyzedPlants = new List<string>();

	private GlobalParameterInfo config => DolocAPI.GlobalParameter;

	private TbPlantDocument plantDocTb => DolocConfig.Tables.TbPlantDocument;

	private int totalPlantCount => unlockedDocuments.Count;

	private int maxPlantCount => plantDocTb.DataList.Count;

	public bool allPlantDocUnlocked => totalPlantCount == maxPlantCount;

	public PlantDocumentManager()
	{
	}

	[JsonConstructor]
	private PlantDocumentManager(List<string> unlockedOrder, List<string> unanalyzedPlants)
	{
		this.unlockedOrder = unlockedOrder;
		this.unanalyzedPlants = unanalyzedPlants;
		foreach (string item in unlockedOrder)
		{
			unlockedDocuments.Add(item, plantDocTb.Get(item));
		}
	}

	public bool CanSubmitItemAsPlant(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (plantDocTb.DataMap.ContainsKey(item.name))
		{
			return !unlockedDocuments.ContainsKey(item.name);
		}
		return false;
	}

	public int SubmitItemAsPlant(Item item)
	{
		if (!CanSubmitItemAsPlant(item))
		{
			return 0;
		}
		Debug.Log("提交植物" + item.name);
		unanalyzedPlants.Add(item.name);
		return 1;
	}

	public void AnalyzePlant()
	{
		latestPlantTitles = "";
		latestPlantRewards = 0;
		latestPlantCount = 0;
		foreach (string unanalyzedPlant in unanalyzedPlants)
		{
			UnlockPlantDocument(unanalyzedPlant);
			Debug.Log("解锁植物" + unanalyzedPlant);
		}
	}

	public bool UnlockPlantDocument(string plantDocId)
	{
		if (!plantDocTb.DataMap.TryGetValue(plantDocId, out var value))
		{
			Debug.LogError("没有配置数据: " + plantDocId);
			return false;
		}
		if (!unlockedDocuments.TryAdd(plantDocId, value))
		{
			Debug.LogWarning("档案已解锁：" + plantDocId);
			return false;
		}
		unlockedOrder.Add(plantDocId);
		DolocAPI.PerformEnvOptimizerBehaviour(EnvOptimizerBehaviourType.UNLOCK_PLANT_DOC);
		latestPlantTitles = latestPlantTitles + "『" + DolocAPI.GetItemTitle(plantDocId) + "』";
		latestPlantRewards += value.Reward;
		latestPlantCount++;
		return true;
	}

	public string GetLatestPlantTitles()
	{
		return latestPlantTitles;
	}

	public int GetLatestPlantReward()
	{
		return latestPlantRewards;
	}

	public int GetLatestPlantCount()
	{
		return latestPlantCount;
	}
}
