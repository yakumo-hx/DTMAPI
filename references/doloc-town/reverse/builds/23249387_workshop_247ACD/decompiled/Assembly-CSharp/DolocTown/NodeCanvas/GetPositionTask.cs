using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇")]
[Name("GetPosition", 0)]
[Description("给定一个Transform,将它的位置赋值给一个Vector3")]
public class GetPositionTask : ActionTask
{
	[RequiredField]
	public BBParameter<Transform> targetTransform;

	[RequiredField]
	public BBParameter<Vector3> targetPosition;

	protected override void OnExecute()
	{
		if (targetTransform.value == null)
		{
			EndAction(success: false);
			return;
		}
		targetPosition.value = targetTransform.value.position;
		EndAction(success: true);
	}
}
