using System.Linq;
using UnityEngine;

namespace RedSaw.Physical;

public class Water2D
{
	private class WaterSpring
	{
		private float velocity;

		private float acceleration;

		private float _offset;

		private float maxOffset;

		public float offset
		{
			get
			{
				return _offset;
			}
			set
			{
				_offset = Mathf.Clamp(value, -1f * maxOffset, maxOffset);
			}
		}

		public WaterSpring(float maxOffset)
		{
			this.maxOffset = Mathf.Abs(maxOffset);
		}

		public void Update(float deltaTime, float springConst, float damping)
		{
			acceleration = (0f - springConst) * offset - damping * velocity;
			velocity += acceleration * deltaTime;
			offset += velocity * deltaTime;
		}

		public void Spread(float deltaX)
		{
			velocity += deltaX;
			offset += deltaX;
		}

		public void Clear()
		{
			acceleration = 0f;
			velocity = 0f;
			offset = 0f;
		}
	}

	private float springConst;

	private float damping;

	private float spread;

	private readonly WaterSpring[] springs;

	private readonly float[] buffer;

	private readonly int maxIndex;

	public int Length => springs.Length;

	public Water2D(int springCount, float maxOffset, float springConst, float damping, float spread)
	{
		this.springConst = springConst;
		this.damping = damping;
		this.spread = spread;
		springs = new WaterSpring[Mathf.Max(3, springCount)];
		for (int i = 0; i < springs.Length; i++)
		{
			springs[i] = new WaterSpring(maxOffset);
		}
		buffer = new float[springCount];
		maxIndex = springs.Length - 1;
	}

	public void SetParams(float springConst, float damping, float spread)
	{
		this.springConst = springConst;
		this.damping = damping;
		this.spread = spread;
	}

	public Vector2[] CalcVertices(Vector2 pos, Vector2 size, float waveScale)
	{
		float num = pos.y + size.y;
		float num2 = size.x / (float)(springs.Length - 1);
		Vector2[] array = new Vector2[springs.Length];
		for (int i = 0; i < springs.Length; i++)
		{
			array[i] = new Vector2(pos.x + (float)i * num2, num + springs[i].offset * waveScale);
		}
		return array;
	}

	public float CalcDistanceErr(Vector2 pos, Vector2 waterPos, float width, float scale)
	{
		if (pos.x < waterPos.x || pos.x > waterPos.x + width)
		{
			return 0f;
		}
		float y = waterPos.y;
		int num = Mathf.FloorToInt((pos.x - waterPos.x) / width * (float)(springs.Length - 1));
		if (num == springs.Length - 1)
		{
			return y + springs[^1].offset * scale - pos.y;
		}
		float num2 = width / (float)(springs.Length - 1);
		Vector2 vector = new Vector2((float)num * num2, y + springs[num].offset * scale);
		float num3 = (springs[num + 1].offset - springs[num].offset) * scale / num2;
		return (pos.x - vector.x) * num3 + vector.y - pos.y;
	}

	public float CalcDistanceError(Vector2 pos, Vector2 waterPos, Vector2 waterSize, float waterScale)
	{
		if (pos.x < waterPos.x || pos.x > waterPos.x + waterSize.x)
		{
			return 0f;
		}
		int num = Mathf.FloorToInt((pos.x - waterPos.x) / waterSize.x * (float)(springs.Length - 1));
		float num2 = waterPos.y + waterSize.y;
		if (num == springs.Length - 1)
		{
			return num2 + springs[^1].offset * waterScale - pos.y;
		}
		float num3 = waterSize.x / (float)(springs.Length - 1);
		Vector2 vector = new Vector2((float)num * num3, num2 + springs[num].offset * waterScale);
		float num4 = (springs[num + 1].offset - springs[num].offset) * waterScale / num3;
		return (pos.x - vector.x) * num4 + vector.y - pos.y;
	}

	public float[] Update(float deltaTime, int simulateTimes = 8)
	{
		for (int i = 0; i < simulateTimes; i++)
		{
			WaterSpring[] array = springs;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].Update(deltaTime, springConst, damping);
			}
			for (int k = 1; k < springs.Length - 1; k++)
			{
				if (k < maxIndex)
				{
					springs[k + 1].Spread((springs[k].offset - springs[k + 1].offset) * spread);
				}
				if (k > 0)
				{
					springs[k - 1].Spread((springs[k].offset - springs[k - 1].offset) * spread);
				}
			}
		}
		for (int l = 0; l < springs.Length; l++)
		{
			buffer[l] = springs[l].offset;
		}
		return buffer;
	}

	public float[] UpdateSeamless(float deltaTime, int simulateTimes = 8)
	{
		for (int i = 0; i < simulateTimes; i++)
		{
			WaterSpring[] array = springs;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].Update(deltaTime, springConst, damping);
			}
			for (int k = 0; k < springs.Length; k++)
			{
				if (k < maxIndex)
				{
					springs[k + 1].Spread((springs[k].offset - springs[k + 1].offset) * spread);
				}
				else
				{
					springs[0].Spread((springs[k].offset - springs[0].offset) * spread);
				}
				if (k > 0)
				{
					springs[k - 1].Spread((springs[k].offset - springs[k - 1].offset) * spread);
				}
				else
				{
					springs[maxIndex].Spread((springs[k].offset - springs[maxIndex].offset) * spread);
				}
			}
		}
		return springs.Select((WaterSpring x) => x.offset).ToArray();
	}

	public void Wave(int index, float distance, float spread = 0.5f)
	{
		if (index == 0)
		{
			springs[0].offset += distance;
			springs[1].offset += distance * spread;
		}
		else if (index == springs.Length - 1)
		{
			springs[^1].offset += distance;
			springs[^1].offset += distance * spread;
		}
		else if (index >= 0 && index < springs.Length)
		{
			springs[index].offset += distance;
			springs[index - 1].offset += distance * spread;
			springs[index + 1].offset += distance * spread;
		}
	}

	public void Clear()
	{
		WaterSpring[] array = springs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Clear();
		}
	}
}
