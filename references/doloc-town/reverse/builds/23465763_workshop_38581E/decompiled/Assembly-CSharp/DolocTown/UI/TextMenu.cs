using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TextMenu : DolocVerticalUI<TextButton>, INavPanel
{
	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Color selectedColor;

	[SerializeField]
	private bool hoverArrowWhenSelected;

	protected float minCellWidth;

	protected override GameObject slotPrefab => LocPfbs.UI_PFB_TEXT_EX;

	public Selectable[] allSelectablesArray => ((IEnumerable<TextButton>)base.slots).Select((Func<TextButton, Selectable>)((TextButton x) => x.button)).ToArray();

	public int allSelectableCount => base.slotCount;

	protected override void __Init()
	{
		base.__Init();
		TextButton[] componentsInChildren = GetComponentsInChildren<TextButton>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.SetActive(value: false);
		}
		minCellWidth = slotLayoutGroup.cellSize.x;
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}

	protected override void OnSlotClick(TextButton slot)
	{
		base.OnSlotClick(slot);
		slot.textColor = selectedColor;
	}

	protected override void OnSlotSelect(TextButton slot)
	{
		base.OnSlotSelect(slot);
		slot.textColor = selectedColor;
		if (hoverArrowWhenSelected)
		{
			slot.GetItemBorder(BorderType.Arrow);
		}
	}

	protected override void OnSlotDeselect(TextButton slot)
	{
		slot.textColor = normalColor;
		if (hoverArrowWhenSelected)
		{
			slot.HideItemBorder();
		}
		base.OnSlotDeselect(slot);
	}

	protected override void OnSlotPointerEnter(TextButton slot)
	{
		base.OnSlotPointerEnter(slot);
		slot.Select();
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		for (int i = 0; i < base.slots.Count; i++)
		{
			base.slots[i].textColor = normalColor;
		}
	}

	public virtual void Render(string[] options)
	{
		Vector2 cellSize = new Vector2(minCellWidth, slotLayoutGroup.cellSize.y);
		SetCapacity(options.Length);
		for (int i = 0; i < base.slots.Count; i++)
		{
			base.slots[i].text = options[i];
			cellSize.x = Mathf.Max(cellSize.x, base.slots[i].preferredWidth);
			base.slots[i].transform.SetSiblingIndex(i);
			base.slots[i].grayed = false;
			base.slots[i].highLighted = false;
		}
		slotLayoutGroup.cellSize = cellSize;
	}
}
