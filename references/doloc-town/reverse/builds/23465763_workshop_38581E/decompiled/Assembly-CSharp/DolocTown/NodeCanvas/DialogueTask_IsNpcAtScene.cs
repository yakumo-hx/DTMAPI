using System;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("检查角色位置", 0)]
[Description("检查指定角色是否处于某个场景(主角外npc暂不支持判roomId)")]
public class DialogueTask_IsNpcAtScene : DialogueConditionTask
{
	private CheckPositionType positionType;

	[SerializeField]
	public string npcName = string.Empty;

	[SerializeField]
	public string sceneName = string.Empty;

	[SerializeField]
	public string roomId = string.Empty;

	public override string taskTitle => positionType switch
	{
		CheckPositionType.Scene => "<" + npcName + ">处于场景<" + sceneName + ">", 
		CheckPositionType.Room => "<" + npcName + ">处于房间<" + roomId + ">", 
		_ => throw new ArgumentOutOfRangeException(), 
	};

	protected override bool CheckCondition()
	{
		if (npcName.IsNullOrEmpty())
		{
			return false;
		}
		if (positionType == CheckPositionType.Room && npcName == "player")
		{
			return DolocAPI.CurrentRoom?.RoomId == roomId;
		}
		return DolocAPI.IsNpcAtScene(npcName, sceneName);
	}
}
