using UnityEngine;

namespace RedSaw.Physical;

public struct PhysicalSimulator
{
	private Vector2 gravity;

	private readonly float forceScale;

	private Vector2 _pos;

	private Vector2 _vel;

	public PhysicalSimulator(Vector2 pos, float forceScale)
	{
		_pos = pos;
		_vel = Vector2.zero;
		gravity = Vector2.down * 9.8f;
		this.forceScale = forceScale;
	}

	public void SetGravity(Vector2 gravity)
	{
		this.gravity = gravity;
	}

	public void Reset(Vector2 pos)
	{
		_pos = pos;
		_vel = Vector2.zero;
	}

	public void AddForce(Vector2 force)
	{
		_vel += force * forceScale;
	}

	public void SetVelocity(Vector2 velocity)
	{
		_vel = velocity;
	}

	public Vector2 Update(float t)
	{
		_vel += gravity * t;
		_pos += _vel * t;
		return _pos;
	}
}
