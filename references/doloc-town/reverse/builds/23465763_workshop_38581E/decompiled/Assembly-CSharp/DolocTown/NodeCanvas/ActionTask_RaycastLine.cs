using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("射线检测", 0)]
[Description("朝着某个方向发射一根射线,并检测是否碰撞到了某个物体")]
public class ActionTask_RaycastLine : ActionTask
{
	public enum RaycastType
	{
		Point,
		Direction
	}

	[RequiredField]
	public RaycastType raycastType;

	[RequiredField]
	public LayerMask layerMask;

	[ShowIf("IsDirectionType", 1)]
	public BBParameter<float> distance;

	[RequiredField]
	public BBParameter<Vector2> direction;

	public bool constraintTag;

	[ShowIf("constraintTag", 1)]
	public BBParameter<string> tag;

	private bool IsDirectionType => raycastType == RaycastType.Direction;

	protected override string info => $"raycast {direction} of {distance}";

	protected override void OnExecute()
	{
		RaycastHit2D raycastHit2D = default(RaycastHit2D);
		switch (raycastType)
		{
		case RaycastType.Point:
		{
			Vector2 vector = direction.value - (Vector2)base.agent.transform.position;
			float magnitude = vector.magnitude;
			raycastHit2D = Physics2D.Raycast(base.agent.transform.position, vector.normalized, magnitude, layerMask);
			break;
		}
		case RaycastType.Direction:
			raycastHit2D = Physics2D.Raycast(base.agent.transform.position, direction.value, distance.value, layerMask);
			break;
		}
		if (raycastHit2D.collider == null)
		{
			EndAction(success: false);
		}
		else if (constraintTag)
		{
			EndAction(raycastHit2D.collider.CompareTag(tag.value));
		}
		else
		{
			EndAction(success: true);
		}
	}
}
