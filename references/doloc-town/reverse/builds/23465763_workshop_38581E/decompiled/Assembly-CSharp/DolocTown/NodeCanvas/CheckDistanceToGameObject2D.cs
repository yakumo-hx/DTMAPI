using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("到目标距离检测", 0)]
[Category("多洛可小镇/怪物")]
public class CheckDistanceToGameObject2D : ConditionTask<Transform>
{
	[RequiredField]
	public BBParameter<Transform> checkTarget;

	public BBParameter<Vector2> offset;

	public CompareMethod checkType = CompareMethod.LessThan;

	public BBParameter<float> distance = 10f;

	[SliderField(0f, 0.1f)]
	public float floatingPoint = 0.05f;

	protected override string info => "Distance" + OperationTools.GetCompareString(checkType) + distance?.ToString() + " to " + checkTarget;

	protected override bool OnCheck()
	{
		return OperationTools.Compare(Vector2.Distance((Vector2)base.agent.position + offset.value, checkTarget.value.transform.position), distance.value, checkType, floatingPoint);
	}

	public override void OnDrawGizmosSelected()
	{
		if (base.agent != null)
		{
			Gizmos.DrawWireSphere((Vector2)base.agent.position + offset.value, distance.value);
		}
	}
}
