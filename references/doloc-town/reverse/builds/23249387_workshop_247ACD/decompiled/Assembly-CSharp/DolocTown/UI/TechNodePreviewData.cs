using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.GameData;
using DolocTown.TreeGraph;
using UnityEngine;

namespace DolocTown.UI;

public struct TechNodePreviewData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public string title { get; }

	public string desc { get; }

	public TechNodeInfoData[] nodeInfoData { get; }

	public TechPointCostData[] pointCostData { get; }

	public string buttonText { get; }

	public bool buttonGrayed { get; }

	public TechNodePreviewData(TreeGraphNode<TechNodeProto> node)
	{
		this = default(TechNodePreviewData);
		if (node != null)
		{
			notEmpty = true;
			icon = node.data.icon;
			title = node.data.Title;
			desc = node.data.Description;
			List<TechNodeInfoData> list = new List<TechNodeInfoData>();
			string[] equipments = node.data.equipments;
			for (int i = 0; i < equipments.Length; i++)
			{
				DolocAPI.QueryEquipment(equipments[i], out var proto);
				list.Add(new TechNodeInfoData(proto));
			}
			equipments = node.data.recipes;
			foreach (string recipe in equipments)
			{
				list.Add(new TechNodeInfoData(recipe));
			}
			equipments = node.data.buildings;
			for (int i = 0; i < equipments.Length; i++)
			{
				DolocAPI.QueryBuilding(equipments[i], out var proto2);
				list.Add(new TechNodeInfoData(proto2));
			}
			nodeInfoData = list.ToArray();
			pointCostData = node.data.costs.Select((TechNodeCost data) => new TechPointCostData(node.id, data)).ToArray();
			bool techNodeUnlockState = DolocAPI.archiveHandle.GetTechNodeUnlockState(node.id);
			bool flag = CheckUnlockCondition(node.data.costs);
			bool flag2 = node.parents.Length == 0 || node.parents.Any((string parent) => DolocAPI.archiveHandle.GetTechNodeUnlockState(parent));
			buttonGrayed = !node.data.isUseful || (!techNodeUnlockState && !flag) || !flag2;
			if (!node.data.isUseful)
			{
				buttonText = DolocConfig.StaticTexts.TechtreeNodeUnopen;
			}
			else if (techNodeUnlockState)
			{
				buttonText = DolocConfig.StaticTexts.TechtreeNodeUnlocked;
			}
			else if (!flag2)
			{
				buttonText = DolocConfig.StaticTexts.TechtreeNodeNotAvaiable;
			}
			else
			{
				buttonText = (flag ? DolocConfig.StaticTexts.TechtreeNodeUnlockConfirm : DolocConfig.StaticTexts.TechtreeNodeLackOfPoints);
			}
		}
	}

	private bool CheckUnlockCondition(TechNodeCost[] costs)
	{
		if (costs.IsNullOrEmpty())
		{
			return true;
		}
		for (int i = 0; i < costs.Length; i++)
		{
			TechNodeCost techNodeCost = costs[i];
			if (DolocAPI.archiveHandle.GetTechPoint(techNodeCost.type) < techNodeCost.count)
			{
				return false;
			}
		}
		return true;
	}
}
