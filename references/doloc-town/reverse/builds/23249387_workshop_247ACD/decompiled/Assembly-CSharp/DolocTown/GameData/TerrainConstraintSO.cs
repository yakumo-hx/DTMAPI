using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct TerrainConstraintSO
{
	[SerializeField]
	public TerrainConstraintList[] constraintLists;

	public readonly TerrainConstraintProto Proto
	{
		get
		{
			if (constraintLists == null)
			{
				return new TerrainConstraintProto(new List<Vector2Int[]>());
			}
			List<Vector2Int[]> list = new List<Vector2Int[]>();
			for (int i = 0; i < constraintLists.Length; i++)
			{
				list.Add(constraintLists[i].positions);
			}
			return new TerrainConstraintProto(list);
		}
	}

	public TerrainConstraintSO(List<Vector2Int[]> constraintSets)
	{
		constraintLists = new TerrainConstraintList[constraintSets.Count];
		for (int i = 0; i < constraintSets.Count; i++)
		{
			constraintLists[i] = new TerrainConstraintList(constraintSets[i]);
		}
	}
}
