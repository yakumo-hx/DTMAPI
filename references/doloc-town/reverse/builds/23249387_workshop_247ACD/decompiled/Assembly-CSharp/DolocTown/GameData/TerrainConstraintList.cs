using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct TerrainConstraintList
{
	[SerializeField]
	public Vector2Int[] positions;

	public TerrainConstraintList(Vector2Int[] positions)
	{
		this.positions = positions;
	}
}
