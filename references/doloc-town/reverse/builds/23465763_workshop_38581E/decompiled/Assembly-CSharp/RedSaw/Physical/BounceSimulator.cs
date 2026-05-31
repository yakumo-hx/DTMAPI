using System;
using UnityEngine;

namespace RedSaw.Physical;

public struct BounceSimulator
{
	private Vector2 pos;

	private Vector2 target;

	private Vector2 gravity;

	private float spring;

	private Vector2 springbackThreshold;

	private float upcastSpeed;

	private bool bounce;

	private Vector2 vel;

	private float time;

	private float totalTime;

	public Action BounceFunc;

	public bool ShouldUpdate => time < totalTime;

	public void InitSimulator(Vector2 start, Vector2 target, Vector2 gravity, float spring, Vector2 threshold, float upcastSpeed, bool bounce)
	{
		pos = start;
		this.target = target;
		this.gravity = gravity;
		this.spring = spring;
		springbackThreshold = threshold;
		this.upcastSpeed = upcastSpeed;
		this.bounce = bounce;
		CalMotionTotalTime();
		vel = new Vector2((target.x - start.x) / totalTime, upcastSpeed);
		time = 0f;
	}

	public Vector2 OnUpdate(float t)
	{
		time += t;
		if (time >= totalTime)
		{
			return pos;
		}
		if (pos.y < target.y && vel.y <= 0f - springbackThreshold.x)
		{
			vel.y *= 0f - spring;
			pos = new Vector2(vel.x * t + pos.x, target.y);
			BounceFunc?.Invoke();
		}
		else
		{
			vel -= gravity * t;
			pos += vel * t;
		}
		return pos;
	}

	private float CalMotionTotalTime()
	{
		float num = upcastSpeed * upcastSpeed / (2f * gravity.y) + pos.y;
		float num2 = upcastSpeed / gravity.y;
		float num3 = (float)Math.Sqrt(2f * Math.Abs(num - target.y) / gravity.y);
		totalTime = num2 + num3;
		if (!bounce)
		{
			return totalTime;
		}
		float num4 = gravity.y * num3;
		if (num4 >= springbackThreshold.y)
		{
			while (num4 >= springbackThreshold.x)
			{
				num4 *= spring;
				totalTime += 2f * num4 / gravity.y;
			}
		}
		return totalTime;
	}
}
