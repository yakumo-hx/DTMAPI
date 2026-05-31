using System;
using UnityEngine;

namespace DolocTown;

public struct SecondOrderDynamics
{
	private Vector2 xp;

	private Vector2 y;

	private Vector2 yd;

	private readonly float k1;

	private readonly float k2;

	private readonly float k3;

	public void SetPrevPosition(Vector2 pos)
	{
		xp = pos;
	}

	public SecondOrderDynamics(float f, float z, float r, Vector2 initPos)
	{
		float num = MathF.PI * f;
		k1 = z / num;
		k2 = 1f / (num * num * 4f);
		k3 = r * z / (num * 2f);
		xp = initPos;
		y = initPos;
		yd = Vector2.zero;
	}

	public Vector2 Update(float delta, Vector2 x)
	{
		Vector2 vector = (x - xp) / delta;
		xp = x;
		y += yd * delta;
		yd += (x + k3 * vector - y - k1 * yd) / k2 * delta;
		return y;
	}
}
