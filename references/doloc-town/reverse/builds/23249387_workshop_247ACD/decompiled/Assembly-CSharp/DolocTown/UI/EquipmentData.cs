using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.GameData;
using DolocTown.TreeGraph;
using Sirenix.Utilities;
using UnityEngine;

namespace DolocTown.UI;

public struct EquipmentData : ICraftData, IUIData
{
	public bool notEmpty { get; }

	public Sprite outputItemSprite { get; }

	public Sprite sceneSprite { get; }

	public string recipeTitle { get; }

	public string outputItemTitle { get; }

	public string description { get; }

	public string subType { get; }

	public string sizeDescription { get; }

	public string buttonText { get; }

	public CostViewerData itemCosts { get; }

	public bool isCostEnough { get; }

	public bool isCollect { get; }

	public int itemCount { get; }

	public Sprite smallCollectIcon { get; }

	public Sprite collectIcon { get; }

	public bool isUnlock { get; }

	public string getUnLockTipText { get; }

	public bool showCompleteInfo { get; }

	public string electronicTip { get; }

	public bool hasTechNode { get; }

	public bool hasBaseEquipment { get; }

	public EquipmentData(string equipmentName, LinearInventory[] Inventories)
	{
		this = default(EquipmentData);
		if (!DolocAPI.QueryEquipment(equipmentName, out var proto))
		{
			return;
		}
		notEmpty = true;
		isUnlock = DolocAPI.archiveHandle.IsEquipmentUnlocked(equipmentName);
		showCompleteInfo = isUnlock || proto.ShowCompleteInfo;
		outputItemSprite = proto.UiSprite;
		sceneSprite = proto.Sprite;
		recipeTitle = (showCompleteInfo ? proto.Title : DolocConfig.StaticTexts.UiOperationTalkUnknown);
		outputItemTitle = recipeTitle;
		description = (showCompleteInfo ? proto.Description : string.Empty);
		if (DolocAPI.QueryItemProto(equipmentName, out var proto2))
		{
			subType = (showCompleteInfo ? proto2.SubType_Ref.Title : string.Empty);
		}
		sizeDescription = (showCompleteInfo ? DolocConfig.StaticTexts.BuildingPanelCoverSizeDescription.Format(proto.CoverSize.x, proto.CoverSize.y) : string.Empty);
		itemCosts = new CostViewerData(proto.CostList, Inventories);
		isCostEnough = itemCosts.isEnough;
		hasBaseEquipment = !proto.BaseEquipment.IsNullOrEmpty() && !DolocAPI.archiveHandle.IsEquipmentUnlocked(proto.BaseEquipment);
		hasTechNode = GetAssociatedTechNode(hasBaseEquipment ? proto.BaseEquipment : equipmentName, out var techTreeTitle, out var nodeTitle);
		getUnLockTipText = (hasTechNode ? DolocUtils.Format(DolocConfig.StaticTexts.EquipmentPanelUnlockTip, techTreeTitle, nodeTitle) : string.Empty);
		buttonText = GetButtonText();
		electronicTip = string.Empty;
		ElectronicComponentProto electronicComponent = proto.ElectronicComponent;
		if (!(electronicComponent is EComProtoAppliance eComProtoAppliance))
		{
			if (!(electronicComponent is EComGeneratorBase eComGeneratorBase))
			{
				if (electronicComponent is EComProtoBattery eComProtoBattery)
				{
					electronicTip = DolocConfig.StaticTexts.EquipmentPanelBatteryTip.Format(eComProtoBattery.Capacity);
				}
			}
			else
			{
				electronicTip = DolocConfig.StaticTexts.EquipmentPanelGeneratorTip.Format(eComGeneratorBase.Efficiency);
			}
		}
		else
		{
			electronicTip = DolocConfig.StaticTexts.EquipmentPanelApplianceTip.Format(eComProtoAppliance.Threshold);
		}
		isCollect = DolocAPI.archiveHandle.farmData.collectEquipments.Contains(equipmentName);
		itemCount = DolocAPI.GetExistItemCount(equipmentName) + GetEquipmentCount(equipmentName);
		smallCollectIcon = (isCollect ? DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_COLLECT_S) : DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_UN_COLLECT_S));
		collectIcon = (isCollect ? DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_COLLECT) : DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_UN_COLLECT));
	}

	private bool GetAssociatedTechNode(string equipmentName, out string techTreeTitle, out string nodeTitle)
	{
		techTreeTitle = string.Empty;
		nodeTitle = string.Empty;
		if (DolocAPI.archiveHandle.IsEquipmentUnlocked(equipmentName))
		{
			return false;
		}
		TreeGraph<TechNodeProto>[] allTreeGraphs = DolocAPI.assets.techTrees.AllTreeGraphs;
		foreach (TreeGraph<TechNodeProto> treeGraph in allTreeGraphs)
		{
			TreeGraphNode<TechNodeProto>[] nodes = treeGraph.nodes;
			for (int j = 0; j < nodes.Length; j++)
			{
				TechNodeProto data = nodes[j].data;
				string[] equipments = data.equipments;
				foreach (string text in equipments)
				{
					if (equipmentName == text)
					{
						techTreeTitle = DolocAPI.GetTechTreeTitle(treeGraph.id);
						nodeTitle = data.Title;
						return true;
					}
				}
			}
		}
		return false;
	}

	private string GetButtonText()
	{
		if (!notEmpty)
		{
			return string.Empty;
		}
		if (hasBaseEquipment && hasTechNode)
		{
			return DolocConfig.StaticTexts.EquipmentPanelJump;
		}
		if (isUnlock)
		{
			if (isCostEnough)
			{
				return DolocConfig.StaticTexts.EquipmentPanelStartBuild;
			}
			return DolocConfig.StaticTexts.BuildingPanelMaterialNotEnough;
		}
		if (!hasTechNode)
		{
			return string.Empty;
		}
		return DolocConfig.StaticTexts.EquipmentPanelJump;
	}

	private int GetEquipmentCount(string id)
	{
		TemplateRoomOutdoor mainFarm = DolocAPI.archiveHandle.farmData.MainFarm;
		int count = mainFarm.DM_equipment.GetEquipmentCount(id);
		mainFarm.DM_building.Buildings.ForEach(delegate(Building build)
		{
			count += build.room.DM_equipment.GetEquipmentCount(id);
		});
		return count;
	}
}
