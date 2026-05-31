using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("档案馆收集进度判断", 0)]
[Description("判断玩家是否全收集芯片或植物")]
public class DialogueTask_CheckDocumentsStatus : DialogueConditionTask
{
	public enum DocumentType
	{
		Plant,
		Chip,
		All
	}

	[SerializeField]
	public DocumentType docType = DocumentType.Chip;

	public override string taskTitle => docType switch
	{
		DocumentType.Plant => "植物档案全解锁", 
		DocumentType.Chip => "芯片档案全解锁", 
		DocumentType.All => "全部档案解锁", 
		_ => null, 
	};

	protected override bool CheckCondition()
	{
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		switch (docType)
		{
		case DocumentType.Plant:
			return documentManager.plantDocMgr.allPlantDocUnlocked;
		case DocumentType.Chip:
			return documentManager.chipDocMgr.allChipDocUnlocked;
		case DocumentType.All:
			if (documentManager.chipDocMgr.allChipDocUnlocked)
			{
				return documentManager.plantDocMgr.allPlantDocUnlocked;
			}
			return false;
		default:
			return false;
		}
	}
}
