using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/城镇")]
[Name("启动或关闭Npc计划表状态", 0)]
public class DialogueTask_SetNpcScheduleStatus : DialogueTask
{
	[SerializeField]
	private string npcName;

	[SerializeField]
	private bool value;

	[SerializeField]
	private string overrideMarkPoint;

	public override string taskTitle => (value ? "启用" : "禁用") + "\"" + npcName + "\"计划表" + (value ? "" : ("(锁定至<" + overrideMarkPoint + ">)"));

	public override void DoAction(Graph graph)
	{
		if (DolocAPI.QueryNpc(npcName, out var npc))
		{
			if (value)
			{
				npc.EnableSchedule();
			}
			else
			{
				npc.DisableSchedule(overrideMarkPoint);
			}
		}
	}
}
