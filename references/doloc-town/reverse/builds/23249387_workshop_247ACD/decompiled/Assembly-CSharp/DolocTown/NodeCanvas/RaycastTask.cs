using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("视线检测", 0)]
[Description("发射射线检测主角是否处于怪物的视野内")]
public class RaycastTask : ConditionTask
{
	[SerializeField]
	[RequiredField]
	public BBParameter<BodyController> target;

	[SerializeField]
	[RequiredField]
	public LayerMask layerMask;

	[SerializeField]
	[RequiredField]
	public BBParameter<float> viewDistance = 10f;

	[SerializeField]
	[RequiredField]
	public string playerTag = "Player";

	protected override bool OnCheck()
	{
		Vector2 vector = base.agent.transform.position;
		Vector2 vector2 = target.value.PositionCenter - vector;
		if (vector2.magnitude > viewDistance.value)
		{
			return false;
		}
		RaycastHit2D raycastHit2D = Physics2D.Raycast(vector, vector2.normalized, viewDistance.value, layerMask);
		if (!(raycastHit2D.collider == null))
		{
			return raycastHit2D.collider.CompareTag(playerTag);
		}
		return false;
	}
}
