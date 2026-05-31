using UnityEngine;

namespace DolocTown.UI;

public interface ICraftData : IUIData
{
	new bool notEmpty { get; }

	Sprite outputItemSprite { get; }

	Sprite sceneSprite { get; }

	string recipeTitle { get; }

	string outputItemTitle { get; }

	string description { get; }

	string buttonText { get; }

	CostViewerData itemCosts { get; }

	bool isCostEnough { get; }
}
