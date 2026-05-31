using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/UI")]
[Name("打开档案馆提交界面", 0)]
[Description("打开档案馆提交界面")]
public class DialogueTask_OpenSubmitPanel : DialogueTask
{
	public ArchiveItemType submitType;

	public override string taskTitle => submitType switch
	{
		ArchiveItemType.Chip => "打开档案馆<芯片>提交界面", 
		ArchiveItemType.Plant => "打开档案馆<植物>提交界面", 
		_ => null, 
	};

	public override void DoAction(Graph graph)
	{
		switch (submitType)
		{
		case ArchiveItemType.Plant:
			DolocAPI.EnterUI<PlantSubmitUiState>();
			break;
		case ArchiveItemType.Chip:
			DolocAPI.EnterUI<ChipSubmitUiState>();
			break;
		default:
			Debug.LogError("未知类型");
			break;
		}
	}
}
