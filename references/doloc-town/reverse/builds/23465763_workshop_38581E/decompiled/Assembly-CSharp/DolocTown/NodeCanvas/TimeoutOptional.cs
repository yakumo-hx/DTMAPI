using NodeCanvas.BehaviourTrees;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("Decorators")]
[Name("TimeoutRevert", 0)]
[Description("Similar to Timeout, but you can choose return status when timeout.")]
[ParadoxNotion.Design.Icon("Timeout", false, "")]
public class TimeoutOptional : Timeout
{
	public Status returnStatus;

	protected override Status OnExecute(Component agent, IBlackboard blackboard)
	{
		if (base.decoratedConnection == null)
		{
			return Status.Optional;
		}
		base.status = base.decoratedConnection.Execute(agent, blackboard);
		if (base.status == Status.Running && base.elapsedTime >= timeout.value)
		{
			base.decoratedConnection.Reset();
			return returnStatus;
		}
		return base.status;
	}
}
