using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("移动", 0)]
[Description("设置移动任务")]
public class ActionTask_MoveTo : ActionTask
{
	[RequiredField]
	public MoveTargetType moveTargetType;

	[RequiredField]
	[ShowIf("IsCustomTarget", 1)]
	public BBParameter<Vector3> customTarget;

	protected bool IsCustomTarget => moveTargetType == MoveTargetType.Custom;

	protected override string info => moveTargetType switch
	{
		MoveTargetType.Custom => $"移动至\"{customTarget}\"", 
		MoveTargetType.Player => "跟随玩家", 
		MoveTargetType.Random => "随机移动", 
		MoveTargetType.Around => "临近移动", 
		_ => string.Empty, 
	};

	protected override void OnExecute()
	{
		MonsterController component = base.agent.GetComponent<MonsterController>();
		if (component == null)
		{
			EndAction(success: false);
		}
		else if (!component.MoveTo(moveTargetType, base.EndAction))
		{
			EndAction(success: false);
		}
	}

	protected override void OnStop(bool interrupted)
	{
		if (interrupted)
		{
			base.agent.GetComponent<MonsterController>().StopMove();
		}
	}
}
