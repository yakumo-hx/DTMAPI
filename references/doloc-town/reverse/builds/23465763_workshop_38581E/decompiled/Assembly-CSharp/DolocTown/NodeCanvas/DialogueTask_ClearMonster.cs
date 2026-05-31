using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("清除怪物", 0)]
[Description("将目标房间的怪物全部移除")]
[Category("多洛可小镇/地牢")]
public class DialogueTask_ClearMonster : DialogueTask
{
	[SerializeField]
	private bool customRoom;

	[SerializeField]
	private string targetRoom;

	public override string taskTitle => "移除房间\"" + (customRoom ? targetRoom : "当前房间") + "\"的所有怪物";

	private IMonsterHost GetMonsterHost()
	{
		if (customRoom)
		{
			return DolocAPI.GetRoom(targetRoom);
		}
		return DolocAPI.CurrentRoom;
	}

	public override void DoAction(Graph graph)
	{
		IMonsterHost monsterHost = GetMonsterHost();
		if (monsterHost == null)
		{
			Debug.LogError("DialogueTask_ClearMonster: 目标房间为空");
		}
		else
		{
			monsterHost.ClearMonsters();
		}
	}
}
