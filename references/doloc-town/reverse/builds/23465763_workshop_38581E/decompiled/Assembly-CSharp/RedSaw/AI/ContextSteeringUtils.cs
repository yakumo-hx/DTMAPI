using System;
using UnityEngine;

namespace RedSaw.AI;

public static class ContextSteeringUtils
{
	public static Vector2[] SeperateDirections(int count)
	{
		Vector2[] array = new Vector2[count];
		for (int i = 0; i < count; i++)
		{
			float num = (float)i * 360f / (float)count;
			array[i] = new Vector2(Mathf.Cos(num * (MathF.PI / 180f)), Mathf.Sin(num * (MathF.PI / 180f)));
		}
		return array;
	}

	public static void CalcInterestMap(Vector2[] dirs, Vector2 targetDir, ref float[] interestMap)
	{
		for (int i = 0; i < dirs.Length; i++)
		{
			interestMap[i] = Vector2.Dot(dirs[i], targetDir);
		}
	}

	public static void CalcDangerMap(Vector2[] dirs, ContextSteeringObstacle[] obsList, ref float[] dgMap, float sensitive = 1f, float maxDanger = 100f)
	{
		for (int i = 0; i < dirs.Length; i++)
		{
			dgMap[i] = 0f;
			for (int j = 0; j < obsList.Length; j++)
			{
				ContextSteeringObstacle contextSteeringObstacle = obsList[j];
				float num = Vector2.Dot(contextSteeringObstacle.errorVec / contextSteeringObstacle.distance, dirs[i]);
				dgMap[i] += sensitive / contextSteeringObstacle.distance * num;
			}
		}
	}
}
