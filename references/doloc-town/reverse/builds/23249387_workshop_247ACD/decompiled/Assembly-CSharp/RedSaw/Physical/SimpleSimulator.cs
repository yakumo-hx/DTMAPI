using UnityEngine;

namespace RedSaw.Physical;

public struct SimpleSimulator
{
	private Vector2 pos;

	private Vector2 vel;

	private Vector2 gravity;

	public Vector2 CurrentPosition
	{
		readonly get
		{
			return pos;
		}
		set
		{
			pos = value;
		}
	}

	public void SetGravity(Vector2 gravity)
	{
		this.gravity = gravity;
	}

	public void SetVelocity(Vector2 velocity)
	{
		vel = velocity;
	}

	public Vector2 OnUpdate(float t)
	{
		vel += gravity * t;
		pos += vel * t;
		return pos;
	}
}
