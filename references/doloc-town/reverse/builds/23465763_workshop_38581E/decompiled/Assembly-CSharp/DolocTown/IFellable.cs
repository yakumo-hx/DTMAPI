using UnityEngine;

namespace DolocTown;

public interface IFellable
{
	bool ShouldCostEnergy { get; }

	bool ShouldCostChopCounter { get; }

	bool OnFell(ItemTool tool, Vector2 hitPosition);
}
