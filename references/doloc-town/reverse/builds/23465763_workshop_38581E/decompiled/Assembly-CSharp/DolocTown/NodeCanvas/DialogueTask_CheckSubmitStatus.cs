using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("档案馆提交道具数量", 0)]
[Description("在【关闭提交页面】后，判断玩家是否成功向档案馆提交道具，以及道具数量情况")]
public class DialogueTask_CheckSubmitStatus : DialogueConditionTask
{
	public enum SubmitItemType
	{
		Plant,
		Chip
	}

	public enum SubmitCountType
	{
		None,
		Single,
		Multiple,
		Overflow
	}

	[SerializeField]
	public SubmitItemType submitItemType = SubmitItemType.Chip;

	public SubmitCountType submitCountType;

	public override string taskTitle => "提交<" + cntText + ">个<" + itemText + ">道具";

	private string itemText => submitItemType switch
	{
		SubmitItemType.Plant => "植物", 
		SubmitItemType.Chip => "芯片", 
		_ => null, 
	};

	private string cntText => submitCountType switch
	{
		SubmitCountType.None => "零", 
		SubmitCountType.Single => "单", 
		SubmitCountType.Multiple => "多", 
		SubmitCountType.Overflow => "超上限", 
		_ => null, 
	};

	protected override bool CheckCondition()
	{
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		if (submitCountType == SubmitCountType.None)
		{
			if (documentManager.latestSubmitItems.IsNullOrEmpty())
			{
				return documentManager.latestOverflowItems.IsNullOrEmpty();
			}
			return false;
		}
		if (documentManager.latestSubmitItems.IsNullOrEmpty())
		{
			return false;
		}
		Item item = documentManager.latestSubmitItems[0];
		switch (submitItemType)
		{
		case SubmitItemType.Plant:
			if (!documentManager.plantDocMgr.CanSubmitItemAsPlant(item))
			{
				return false;
			}
			break;
		case SubmitItemType.Chip:
			if (!documentManager.chipDocMgr.CanSubmitItemAsChip(item))
			{
				return false;
			}
			break;
		default:
			Debug.LogError("未知的提交类型: " + item.name);
			return false;
		}
		switch (submitCountType)
		{
		case SubmitCountType.Single:
			if (documentManager.latestSubmitItems.Length == 1 && item.count == 1)
			{
				return documentManager.latestOverflowItems.IsNullOrEmpty();
			}
			return false;
		case SubmitCountType.Multiple:
			if (documentManager.latestSubmitItems.Length > 1 || item.count > 1)
			{
				return documentManager.latestOverflowItems.IsNullOrEmpty();
			}
			return false;
		case SubmitCountType.Overflow:
			if (documentManager.latestSubmitItems.Length > 1)
			{
				return documentManager.latestOverflowItems.Length > 1;
			}
			return false;
		default:
			return false;
		}
	}
}
