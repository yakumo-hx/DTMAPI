using UnityEngine;

namespace DolocTown;

public interface ICelledSprite
{
	Vector2Int cellPosition { get; }

	Vector2Int cellAnchor { get; }

	Vector2 worldPosition { get; }

	Vector2 worldAnchor { get; }
}
