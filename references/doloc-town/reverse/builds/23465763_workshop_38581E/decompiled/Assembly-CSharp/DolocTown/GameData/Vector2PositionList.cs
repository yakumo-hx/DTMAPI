using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct Vector2PositionList
{
	[SerializeField]
	public Vector2[] positions;

	public Vector2PositionList(Vector2[] positions)
	{
		this.positions = positions;
	}
}
