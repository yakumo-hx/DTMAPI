using DolocTown.Config;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct TechPointCostData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public string count { get; }

	public bool grayed { get; }

	public TechPointCostData(string nodeName, TechNodeCost nodeCost)
	{
		this = default(TechPointCostData);
		notEmpty = true;
		TechPointInfo orDefault = DolocConfig.Tables.TbTechPoint.GetOrDefault(nodeCost.type);
		icon = orDefault.Icon.Asset;
		count = "x" + nodeCost.count;
		grayed = DolocAPI.archiveHandle.GetTechNodeUnlockState(nodeName) || DolocAPI.archiveHandle.GetTechPoint(nodeCost.type) < nodeCost.count;
	}
}
