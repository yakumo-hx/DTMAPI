using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RedSaw.Physical;

public class Rope2D
{
	private class RopePoint
	{
		private Vector2 now;

		private Vector2 old;

		public Vector2 Position
		{
			get
			{
				return now;
			}
			set
			{
				now = value;
			}
		}

		public void Update(Vector2 gravity)
		{
			Vector2 vector = now - old;
			old = now;
			now += vector + gravity;
		}

		public void ResetPosition(Vector2 position)
		{
			now = position;
			old = position;
		}
	}

	private readonly RopePoint[] points;

	private Vector2 lockPos;

	private Vector2 lockPosEnd;

	private Vector2 G;

	private float baseL;

	private float spconst;

	public Vector2 LockedPosition
	{
		get
		{
			return lockPos;
		}
		set
		{
			lockPos = value;
		}
	}

	public Vector2 LockedPositionEnd
	{
		get
		{
			return lockPosEnd;
		}
		set
		{
			lockPosEnd = value;
		}
	}

	public float baseLen
	{
		set
		{
			baseL = value;
		}
	}

	public float gravity
	{
		set
		{
			G = new Vector2(0f, 0f - value);
		}
	}

	public float springConst
	{
		get
		{
			return spconst;
		}
		set
		{
			spconst = value;
		}
	}

	public int Count => points.Length;

	public IEnumerable<Vector2> Positions => points.Select((RopePoint pt) => pt.Position);

	public Vector2 EndPosition => points[^1].Position;

	public Rope2D(int count, float stdL, float springConst, Vector2 gravity)
	{
		baseL = stdL;
		G = gravity;
		spconst = springConst;
		points = new RopePoint[count];
		for (int i = 0; i < count; i++)
		{
			points[i] = new RopePoint();
		}
	}

	public void ReloadPositionsFromLine(Vector2 from, Vector2 to)
	{
		Vector2 vector = to - from;
		float num = vector.magnitude / (float)(points.Length - 1);
		Vector2 normalized = vector.normalized;
		for (int i = 0; i < points.Length; i++)
		{
			points[i].ResetPosition(from + normalized * num * i);
		}
		points[0].ResetPosition(from);
		points[^1].ResetPosition(to);
	}

	public Vector3[] PositionArray(float zposition)
	{
		Vector3[] array = new Vector3[points.Length];
		for (int i = 0; i < array.Length; i++)
		{
			Vector2 position = points[i].Position;
			array[i] = new Vector3(position.x, position.y, zposition);
		}
		return array;
	}

	public void UpdateLockDouble(float deltaTime)
	{
		Vector2 vector = G * deltaTime;
		for (int i = 1; i < points.Length; i++)
		{
			points[i].Update(vector);
		}
		for (int j = 0; j < 10; j++)
		{
			ConstraintDoubleSide();
		}
	}

	public void UpdateLockHead(float deltaTime)
	{
		Vector2 vector = G * deltaTime;
		for (int i = 1; i < points.Length; i++)
		{
			points[i].Update(vector);
		}
		for (int j = 0; j < 10; j++)
		{
			Constraint();
		}
	}

	private void ConstraintDoubleSide()
	{
		points[0].Position = lockPos;
		points[^1].Position = lockPosEnd;
		int num = points.Length - 1;
		for (int i = 0; i < num; i++)
		{
			RopePoint ropePoint = points[i];
			RopePoint ropePoint2 = points[i + 1];
			Vector2 vector = ropePoint.Position - ropePoint2.Position;
			float num2 = Mathf.Abs(vector.magnitude - baseL);
			Vector2 vector2 = vector.normalized * (Mathf.Sign(num2) * num2 * spconst);
			if (i != 0)
			{
				vector2 *= 0.5f;
				ropePoint.Position -= vector2;
				ropePoint2.Position += vector2;
			}
			else
			{
				ropePoint2.Position += vector2;
			}
		}
	}

	private void Constraint()
	{
		points[0].Position = lockPos;
		int num = points.Length - 1;
		for (int i = 0; i < num; i++)
		{
			RopePoint ropePoint = points[i];
			RopePoint ropePoint2 = points[i + 1];
			Vector2 vector = ropePoint.Position - ropePoint2.Position;
			float num2 = Mathf.Abs(vector.magnitude - baseL);
			Vector2 vector2 = vector.normalized * (Mathf.Sign(num2) * num2 * spconst);
			if (i != 0)
			{
				vector2 *= 0.5f;
				ropePoint.Position -= vector2;
				ropePoint2.Position += vector2;
			}
			else
			{
				ropePoint2.Position += vector2;
			}
		}
	}
}
