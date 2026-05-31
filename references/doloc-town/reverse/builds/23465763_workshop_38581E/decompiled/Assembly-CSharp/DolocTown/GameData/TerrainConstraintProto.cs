using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown.GameData;

public readonly struct TerrainConstraintProto
{
	public static readonly TerrainConstraintProto Empty;

	private readonly List<Vector2Int[]> constraintSets;

	public Vector2Int[] this[Enum index]
	{
		get
		{
			if (constraintSets == null || constraintSets.Count == 0)
			{
				return Array.Empty<Vector2Int>();
			}
			if (index.GetHashCode() < constraintSets.Count)
			{
				return constraintSets[index.GetHashCode()];
			}
			return Array.Empty<Vector2Int>();
		}
	}

	public Vector2Int[] this[int index]
	{
		get
		{
			if (constraintSets == null || constraintSets.Count == 0)
			{
				return Array.Empty<Vector2Int>();
			}
			return constraintSets[index];
		}
	}

	public bool Contains(Vector2Int pos, int index)
	{
		return this[index].Contains(pos);
	}

	public TerrainConstraintProto(List<Vector2Int[]> constraintSets = null)
	{
		this.constraintSets = constraintSets ?? new List<Vector2Int[]>();
	}
}
