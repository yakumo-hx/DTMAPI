using UnityEngine;

namespace DolocTown;

[GameEntityManager("/global/floating_object/simple", DolocGameAssets.GAME_ENTITY_FLOATING_OBJECT_SIMPLE, Frequency = 10)]
public class FloatingObjectSimple : FloatingObjectBase
{
	private float _velocity;

	private float _acceleration;

	private void FixedUpdate()
	{
		if (!(_controller == null))
		{
			Vector3 vector = base.transform.position;
			float num = Mathf.Clamp(_controller.CalcDistanceError(vector), 0f - floatingParam.dstClip, floatingParam.dstClip);
			_acceleration = num * base.springConst;
			_acceleration -= _velocity * base.damping;
			_velocity += _acceleration * Time.fixedDeltaTime;
			vector += new Vector3(0f, _velocity * Time.fixedDeltaTime, 0f);
			base.transform.position = vector;
		}
	}

	public override void ResetFloatObject()
	{
		_velocity = 0f;
		_acceleration = 0f;
	}
}
