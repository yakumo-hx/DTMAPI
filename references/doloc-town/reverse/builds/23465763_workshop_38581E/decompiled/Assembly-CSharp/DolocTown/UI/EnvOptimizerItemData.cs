using UnityEngine;

namespace DolocTown.UI;

public struct EnvOptimizerItemData : IUIData
{
	public bool notEmpty { get; }

	public string title { get; }

	public Sprite sprite { get; }

	public bool isActive { get; }

	public EnvOptimizerItemData(EnvOptimizerComponentSlot slot)
	{
		this = default(EnvOptimizerItemData);
		if (slot?.CurrentItem != null)
		{
			notEmpty = true;
			title = slot.CurrentItem.title;
			sprite = slot.CurrentItem.uiSprite;
			isActive = slot.IsActive;
		}
	}
}
