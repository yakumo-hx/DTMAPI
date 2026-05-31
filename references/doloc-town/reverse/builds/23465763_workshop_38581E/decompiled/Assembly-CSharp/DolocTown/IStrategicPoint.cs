using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public interface IStrategicPoint
{
	Vector2 Position { get; }

	float Radius { get; }

	List<Transform> LockedTransforms { get; }

	bool HasEnemy => LockedTransforms.Count > 0;
}
