using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using DolocTown.TreeGraph;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class TechLevelManager
{
	[JsonProperty]
	private Dictionary<TechPointType, TechLevelData> levelDatas;

	[JsonConstructor]
	public TechLevelManager(Dictionary<TechPointType, TechLevelData> levelDatas = null)
	{
		this.levelDatas = levelDatas ?? new Dictionary<TechPointType, TechLevelData>();
		foreach (TechPointType value in Enum.GetValues(typeof(TechPointType)))
		{
			this.levelDatas.TryAdd(value, new TechLevelData(value));
		}
	}

	public void AfterLoadData()
	{
		foreach (TechLevelData value in levelDatas.Values)
		{
			value.ResetAvailablePoints();
		}
		TreeGraph<TechNodeProto>[] allTreeGraphs = DolocAPI.assets.techTrees.AllTreeGraphs;
		foreach (TreeGraph<TechNodeProto> treeGraph in allTreeGraphs)
		{
			foreach (string unlockedTechNode in DolocAPI.archiveHandle.farmData.unlockedTechNodes)
			{
				if (treeGraph.QueryNode(unlockedTechNode, out var node))
				{
					TechNodeCost[] costs = node.data.costs;
					for (int j = 0; j < costs.Length; j++)
					{
						TechNodeCost techNodeCost = costs[j];
						ChangeTechPoint(techNodeCost.type, -1 * techNodeCost.count);
					}
				}
			}
		}
	}

	public TechLevelData GetLevelData(TechPointType type)
	{
		return levelDatas[type];
	}

	public TechLevelData[] GetAllLevelData()
	{
		return levelDatas.Values.OrderByDescending((TechLevelData x) => x.proto.Order).ToArray();
	}

	public bool ChangeTechExp(TechPointType type, int exp, out TechLevelData levelData)
	{
		levelData = levelDatas[type];
		return levelData.AddTechExp(exp);
	}

	public void ChangeTechPoint(TechPointType type, int point)
	{
		levelDatas[type].ChangeTechPoint(point);
	}

	public void ClampExp(TechPointType type, int maxExp)
	{
		int totalExp = levelDatas[type].TotalExp;
		levelDatas[type].__Clear();
		levelDatas[type].AddTechExp(Mathf.Min(totalExp, maxExp));
	}
}
