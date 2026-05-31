using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct PlatformPositionInfo
{
	[SerializeField]
	public Vector2Int lb;

	[SerializeField]
	public Vector2Int rb;

	[SerializeField]
	public Vector2Int rt;
}
