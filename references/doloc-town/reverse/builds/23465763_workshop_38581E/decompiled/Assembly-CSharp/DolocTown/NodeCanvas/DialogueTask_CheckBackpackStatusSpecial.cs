using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("检查背包是否包含档案馆物品", 0)]
[Description("检查背包是否包含档案馆物品")]
public class DialogueTask_CheckBackpackStatusSpecial : DialogueConditionTask
{
	public enum BackpackItemType
	{
		Plant,
		Chip
	}

	public BackpackItemType backpackItemType;

	private string title => backpackItemType switch
	{
		BackpackItemType.Plant => "澳柯玛植物", 
		BackpackItemType.Chip => "芯片", 
		_ => null, 
	};

	public override string taskTitle => "背包拥有<" + title + ">";

	private LinearInventory backpack => DolocAPI.archiveHandle.InventorySystem.inventory;

	protected override bool CheckCondition()
	{
		switch (backpackItemType)
		{
		case BackpackItemType.Plant:
			return HasPlant();
		case BackpackItemType.Chip:
			return HasChip();
		default:
			Debug.LogError("未知类型");
			return false;
		}
	}

	private bool HasPlant()
	{
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		for (int i = 0; i < backpack.capacity; i++)
		{
			Item item = backpack.Read(i);
			if (documentManager.plantDocMgr.CanSubmitItemAsPlant(item))
			{
				return true;
			}
		}
		return false;
	}

	private bool HasChip()
	{
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		for (int i = 0; i < backpack.capacity; i++)
		{
			Item item = backpack.Read(i);
			if (documentManager.chipDocMgr.CanSubmitItemAsChip(item))
			{
				return true;
			}
		}
		return false;
	}
}
