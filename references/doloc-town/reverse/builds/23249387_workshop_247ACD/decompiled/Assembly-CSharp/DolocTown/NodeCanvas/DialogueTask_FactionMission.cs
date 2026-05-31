using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/任务")]
[Name("部署势力任务", 0)]
[Description("指定一个势力任务Id并添加到玩家的面板中")]
public class DialogueTask_FactionMission : DialogueTask
{
	[SerializeField]
	[RequiredField]
	public string missionId;

	public override string taskTitle => "部署势力任务<" + missionId + ">";

	public override void DoAction(Graph graph)
	{
		DolocAPI.archiveHandle.StartFactionMission(missionId);
	}
}
