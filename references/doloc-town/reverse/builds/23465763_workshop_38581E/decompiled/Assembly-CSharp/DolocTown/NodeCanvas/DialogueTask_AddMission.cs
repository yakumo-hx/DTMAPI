using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/任务")]
[Name("部署任务链", 0)]
[Description("指定一个任务链Id并添加到玩家的任务面板")]
public class DialogueTask_AddMission : DialogueTask
{
	[SerializeField]
	[RequiredField]
	public string missionId;

	[SerializeField]
	[RequiredField]
	public bool noRepeat;

	public override string taskTitle => "启动任务链<" + missionId + ">";

	public override void DoAction(Graph graph)
	{
		if (DolocAPI.assets.missionChains.QueryData(missionId, out var data))
		{
			if (!noRepeat || !DolocAPI.archiveHandle.farmData.missionChainManager.IsChainCompleted(missionId))
			{
				DolocAPI.archiveHandle.StartMissionChain(data);
			}
		}
		else
		{
			string text = "任务链" + missionId + "没有找到";
			DolocAPI.ShowMessageBoxSmallErr(text);
			DolocAPI.outputError(text);
		}
	}
}
