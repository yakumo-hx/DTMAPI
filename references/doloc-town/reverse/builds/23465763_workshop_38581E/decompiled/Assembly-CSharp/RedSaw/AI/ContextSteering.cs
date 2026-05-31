using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.AI;

public struct ContextSteering
{
	public Vector2[] dirs;

	public float[] interestMap;

	public float[] dangerMap;

	public Vector2 lastDir;

	public IEnumerable<Vector2> dangerVecs
	{
		get
		{
			for (int i = 0; i < dirs.Length; i++)
			{
				yield return dirs[i] * dangerMap[i];
			}
		}
	}

	public ContextSteering(int count)
	{
		dirs = ContextSteeringUtils.SeperateDirections(count);
		interestMap = new float[count];
		dangerMap = new float[count];
		lastDir = Vector2.zero;
	}

	public void Clear()
	{
		for (int i = 0; i < dirs.Length; i++)
		{
			interestMap[i] = 0f;
			dangerMap[i] = 0f;
		}
	}

	public Vector2 CalcContextDir(Vector2 targetDir, ContextSteeringObstacle[] obstacles, float sensitive = 1f)
	{
		if (lastDir != targetDir)
		{
			lastDir = targetDir;
			ContextSteeringUtils.CalcInterestMap(dirs, targetDir, ref interestMap);
		}
		ContextSteeringUtils.CalcDangerMap(dirs, obstacles, ref dangerMap, sensitive);
		Vector2 zero = Vector2.zero;
		for (int i = 0; i < dirs.Length; i++)
		{
			zero += dirs[i] * (interestMap[i] - dangerMap[i]);
		}
		return zero;
	}
}
