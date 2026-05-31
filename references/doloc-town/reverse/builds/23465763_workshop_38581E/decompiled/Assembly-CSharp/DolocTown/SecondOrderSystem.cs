using UnityEngine;

namespace DolocTown;

public class SecondOrderSystem : DolocObject
{
	[SerializeField]
	[Range(0f, 5f)]
	public float frequency;

	[SerializeField]
	[Range(0f, 3f)]
	public float damping;

	[SerializeField]
	[Range(-3f, 3f)]
	public float react;

	[SerializeField]
	[Tooltip("当与目标的距离超过该阈值时，直接重置位置")]
	public float resetThreshold = 30f;

	private Transform target;

	private Vector3 targetOffset;

	private SecondOrderDynamics dynamics;

	private Vector3 TargetPosition
	{
		get
		{
			Vector3 result = target.position + targetOffset;
			result.z = base.transform.position.z;
			return result;
		}
	}

	public void SetTarget(Transform target, Vector2 offset)
	{
		this.target = target;
		targetOffset = new Vector3(offset.x, offset.y, 0f);
		ResetSecondOrderSystem();
	}

	public void ResetSecondOrderSystem()
	{
		Vector2 initPos = Vector2.zero;
		if (target != null)
		{
			initPos = target.position;
		}
		dynamics = new SecondOrderDynamics(frequency, damping, react, initPos);
	}

	public float OnFixedUpdate(float deltaTime)
	{
		if (target == null)
		{
			return 0f;
		}
		if (Vector2.Distance(target.position, base.transform.position) > resetThreshold)
		{
			ResetPosition();
			return 0f;
		}
		Vector2 vector = dynamics.Update(deltaTime, target.position);
		float result = vector.x - base.transform.position.x;
		base.transform.position = new Vector3(vector.x, vector.y, base.transform.position.z);
		return result;
	}

	public void ResetPosition()
	{
		if (!(target == null))
		{
			dynamics = new SecondOrderDynamics(frequency, damping, react, TargetPosition);
			base.transform.position = TargetPosition;
		}
	}

	public void ResetPosition(Vector2 position)
	{
		if (!(target == null))
		{
			dynamics = new SecondOrderDynamics(frequency, damping, react, position);
			Vector3 vector = new Vector3(position.x, position.y, base.transform.position.z);
			base.transform.position = vector;
		}
	}

	public void SetFrequency(float value)
	{
		frequency = value;
		if (!(target == null))
		{
			dynamics = new SecondOrderDynamics(frequency, damping, react, TargetPosition);
			base.transform.position = TargetPosition;
		}
	}
}
