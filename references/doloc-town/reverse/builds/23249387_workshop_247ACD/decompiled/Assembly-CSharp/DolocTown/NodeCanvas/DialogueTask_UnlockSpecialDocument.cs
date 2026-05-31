using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/成就")]
[Name("解锁指定档案", 0)]
[Description("解锁指定id的档案")]
public class DialogueTask_UnlockSpecialDocument : DialogueTask
{
	public ArchiveItemType submitType = ArchiveItemType.Chip;

	public string docId;

	public override string taskTitle => submitType switch
	{
		ArchiveItemType.Chip => "解锁<芯片>档案<" + docId + ">", 
		ArchiveItemType.Plant => "解锁<植物>档案<" + docId + ">", 
		_ => null, 
	};

	public override void DoAction(Graph graph)
	{
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		switch (submitType)
		{
		case ArchiveItemType.Plant:
			documentManager.plantDocMgr.UnlockPlantDocument(docId);
			break;
		case ArchiveItemType.Chip:
			documentManager.chipDocMgr.UnlockChipDocumentById(docId);
			break;
		default:
			Debug.LogError("未知类型");
			break;
		}
	}
}
