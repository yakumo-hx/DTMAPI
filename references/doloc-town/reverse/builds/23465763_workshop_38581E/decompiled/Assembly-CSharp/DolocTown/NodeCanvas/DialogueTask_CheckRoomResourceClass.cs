using DolocTown.Config.Resource;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("检查指定房间剩余资源数量", 0)]
[Description("检查指定房间剩余资源数量是否符合条件")]
public class DialogueTask_CheckRoomResourceClass : DialogueConditionTask
{
	[SerializeField]
	public string roomId = "";

	[SerializeField]
	public DungeonResourceClass resourceClass;

	[SerializeField]
	public int value = 10;

	[SerializeField]
	public bool needRender;

	[SerializeField]
	public CompareMethod compareMethod = CompareMethod.LessThan;

	public override string taskTitle => $"<{roomId}>中<{resourceClass}>类型的资源数量 {compareMethod.CompareLabel()} {value}";

	protected override bool CheckCondition()
	{
		DolocAPI.QueryRoom(roomId, out var room);
		int num = 0;
		foreach (DungeonResource allDungeonResource in room.DM_dungeonResource.AllDungeonResources)
		{
			if (allDungeonResource.Proto.ResourceClass == resourceClass && (!needRender || allDungeonResource.isRender))
			{
				num++;
			}
		}
		return OperationTools.Compare(num, value, compareMethod);
	}
}
