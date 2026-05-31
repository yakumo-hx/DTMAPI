using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/任务")]
[Name("部署任务掉落物", 0)]
[Description("指定房间和标记点来生成一个任务掉落物")]
public class DialogueTask_AddMissionItem : DialogueTask
{
	public enum MissionItemHostType
	{
		CityOrSuburb,
		Dungeon
	}

	[SerializeField]
	public MissionItemHostType roomType;

	[SerializeField]
	public string missionItemName;

	[SerializeField]
	public string sceneName;

	[SerializeField]
	public string roomName;

	[SerializeField]
	public string markPointName;

	public override string taskTitle => "添加任务掉落物<" + missionItemName + ">";

	public override void DoAction(Graph graph)
	{
		if (DolocAPI.CreateMissionItem(missionItemName, markPointName))
		{
			DolocAPI.outputSuccess("任务道具\"" + missionItemName + "\"创建成功");
		}
		else
		{
			DolocAPI.outputError("任务道具\"" + missionItemName + "\"创建失败");
		}
	}
}
