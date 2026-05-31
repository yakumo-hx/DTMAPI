using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public readonly struct PlatformCutInfos
{
	public readonly Dictionary<Platform, List<Vector2Int>> CollisionPlatforms;

	public IEnumerable<Vector2Int> AllCutPositions
	{
		get
		{
			foreach (KeyValuePair<Platform, List<Vector2Int>> kv in CollisionPlatforms)
			{
				foreach (Vector2Int item in kv.Value)
				{
					foreach (Vector2Int columnPositionsFromPo in kv.Key.geometry.GetColumnPositionsFromPos(item))
					{
						yield return columnPositionsFromPo;
					}
				}
			}
		}
	}

	public PlatformCutInfos(Dictionary<Platform, List<Vector2Int>> collisionPlatforms)
	{
		CollisionPlatforms = collisionPlatforms;
	}
}
