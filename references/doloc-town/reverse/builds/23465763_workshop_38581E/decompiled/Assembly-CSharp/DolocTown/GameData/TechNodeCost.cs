using DolocTown.Config.TechTree;
using UnityEngine;

namespace DolocTown.GameData;

public readonly struct TechNodeCost
{
	public readonly TechPointType type;

	public readonly int count;

	public TechNodeCost(TechPointType type, int count)
	{
		this.type = type;
		this.count = Mathf.Max(1, count);
	}
}
