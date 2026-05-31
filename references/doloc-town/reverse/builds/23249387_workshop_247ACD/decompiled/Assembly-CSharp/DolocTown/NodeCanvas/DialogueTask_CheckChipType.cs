using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("包含指定芯片", 0)]
[Description("在【解析芯片】前，判断待解析的芯片是否包括普通芯片或特殊芯片")]
public class DialogueTask_CheckChipType : DialogueConditionTask
{
	public enum ChipType
	{
		Normal,
		Special
	}

	[SerializeField]
	public ChipType chipType;

	public override string taskTitle => chipType switch
	{
		ChipType.Normal => "包含<普通>芯片", 
		ChipType.Special => "包含<特殊>芯片", 
		_ => null, 
	};

	protected override bool CheckCondition()
	{
		ChipDocumentManager chipDocMgr = DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr;
		switch (chipType)
		{
		case ChipType.Normal:
			return chipDocMgr.HasNormalChipToAnalyze();
		case ChipType.Special:
			return chipDocMgr.HasSpecialChipToAnalyze();
		default:
			Debug.LogError("未知类型");
			return false;
		}
	}
}
