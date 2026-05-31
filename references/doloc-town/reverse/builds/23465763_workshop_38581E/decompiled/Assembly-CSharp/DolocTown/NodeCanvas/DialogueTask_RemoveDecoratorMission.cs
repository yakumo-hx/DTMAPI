using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/任务")]
[Name("【弃用】移除装饰器任务", 0)]
[Description("【弃用】指定一个装饰器任务并移除")]
public class DialogueTask_RemoveDecoratorMission : DialogueTask
{
	[SerializeField]
	[RequiredField]
	public string missionId;

	public override string taskTitle => "移除装饰器任务<" + missionId + ">";

	public override void DoAction(Graph graph)
	{
	}
}
