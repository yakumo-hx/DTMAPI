using UnityEngine;

namespace DolocTown.UI;

public abstract class VerticalButtonGroup<T> : DolocVerticalUI<T> where T : DolocNavigationButton
{
	[SerializeField]
	private bool hoverArrowWhenSelected;

	protected override bool disableRecycleOnInit => true;

	protected override GameObject slotPrefab => GetComponentInChildren<T>().gameObject;

	protected override void __Init()
	{
		base.__Init();
		SetCapacity(base.slotCount);
	}

	protected override void OnSlotSelect(T slot)
	{
		base.OnSlotSelect(slot);
		DolocAPI.UIRaiseRoll();
		if (hoverArrowWhenSelected)
		{
			slot.GetItemBorder(BorderType.Arrow);
		}
	}

	protected override void OnSlotDeselect(T slot)
	{
		base.OnSlotDeselect(slot);
		if (hoverArrowWhenSelected)
		{
			slot.HideItemBorder();
		}
	}

	public override void SetCapacity(int _)
	{
		base.totalCapacity = base.slotCount;
		lineCapacity = 1;
		base.rowCount = base.slotCount;
		SetClickCallbacks(clickCallback);
		SetSelectCallbacks(selectCallback);
		SetDeselectCallbacks(deselectCallback);
		SetPointerEnterCallbacks(pointerEnterCallback);
		SetPointerExitCallbacks(pointerExitCallback);
		BuildNavigation();
	}
}
