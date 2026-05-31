using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/成就")]
[Name("【结束节点】处理已提交的档案", 0)]
[Description("根据类型和已向档案馆提交的道具,解锁对应档案,并触发特殊事件")]
public class DialogueTask_UnlockDocument : DialogueTask
{
	public ArchiveItemType submitType;

	public override string taskTitle => submitType switch
	{
		ArchiveItemType.Chip => "解锁<芯片>档案及特殊对话", 
		ArchiveItemType.Plant => "解锁<植物>档案", 
		_ => null, 
	};

	public override void DoAction(Graph graph)
	{
		DocumentManager documentManager = DolocAPI.archiveHandle.cityData.documentManager;
		switch (submitType)
		{
		case ArchiveItemType.Plant:
			documentManager.plantDocMgr.AnalyzePlant();
			break;
		case ArchiveItemType.Chip:
			documentManager.chipDocMgr.AnalyzeChips();
			break;
		default:
			Debug.LogError("未知类型");
			break;
		}
	}
}
