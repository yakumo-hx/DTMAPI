using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw;

public static class Vector2D
{
	public static Vector2 RotateDeg(this Vector2 v, float angle)
	{
		angle *= MathF.PI / 180f;
		float num = Mathf.Sin(angle);
		float num2 = Mathf.Cos(angle);
		return new Vector2(v.x * num2 - v.y * num, v.x * num + v.y * num2);
	}

	public static float EulerAngle(this Vector2 dir)
	{
		return Mathf.Atan2(dir.y, dir.x) * 57.29578f;
	}

	public static Vector2 Noise(this Vector2 dir, float accuracy, float noiseRange = 0.25f)
	{
		if (accuracy >= 1f)
		{
			return dir;
		}
		float num = MathF.PI * noiseRange * (1f - accuracy);
		float f = UnityEngine.Random.Range(0f - num, num);
		return new Vector2(dir.x * Mathf.Cos(f) + dir.y * Mathf.Sin(f), (0f - dir.x) * Mathf.Sin(f) + dir.y * Mathf.Cos(f));
	}

	public static IEnumerable<Vector2> SplitIntoSector(this Vector2 dir, int splitCount, float sectorAngle)
	{
		if (splitCount <= 0)
		{
			yield break;
		}
		if (splitCount == 1)
		{
			yield return dir;
			yield break;
		}
		sectorAngle = Mathf.Clamp(sectorAngle, 0f, 360f);
		float num = sectorAngle * (MathF.PI / 180f);
		float half = num * 0.5f;
		float interval = num / (float)splitCount;
		for (int i = 0; i < splitCount; i++)
		{
			float f = 0f - half + (float)i * interval;
			float num2 = Mathf.Cos(f);
			float num3 = Mathf.Sin(f);
			yield return new Vector2(dir.x * num2 - dir.y * num3, dir.x * num3 + dir.y * num2);
		}
	}
}
