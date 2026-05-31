using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public interface IAnimalFence
{
	IEnumerable<Vector2Int> FencePositions { get; }
}
