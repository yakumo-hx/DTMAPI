using UnityEngine;
using UnityEngine.Events;

namespace DolocTown.UI;

public class TitleMenu : DolocHorizontalUI<TextButton>
{
	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Color selectedColor;

	[SerializeField]
	private Color normalColorBG;

	[SerializeField]
	private Color selectedColorBG;

	[SerializeField]
	private DolocButtonComponent leftButton;

	[SerializeField]
	private DolocButtonComponent rightButton;

	protected float minCellWidth;

	public UnityEvent onAnyClicked = new UnityEvent();

	protected override GameObject slotPrefab => LocPfbs.UI_PFB_TEXT_EX;

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
		leftButton.onClick.AddListener(delegate
		{
			SelectPrev();
			onAnyClicked.Invoke();
		});
		rightButton.onClick.AddListener(delegate
		{
			SelectNext();
			onAnyClicked.Invoke();
		});
	}

	protected override void OnSlotClick(TextButton slot)
	{
		base.OnSlotClick(slot);
		RefreshSlotColor();
		onAnyClicked.Invoke();
	}

	protected override void OnSlotSelect(TextButton slot)
	{
		base.OnSlotSelect(slot);
		RefreshSlotColor();
		DolocAPI.UIRaiseRoll();
	}

	private void RefreshSlotColor()
	{
		foreach (TextButton slot in base.slots)
		{
			SetColor(slot, slot.index == base.selectedIndex);
		}
	}

	private void SetColor(TextButton slot, bool highLighted)
	{
		slot.textColor = (highLighted ? selectedColor : normalColor);
		slot.backgroundColor = (highLighted ? selectedColorBG : normalColorBG);
	}

	public virtual void Render(string[] labels)
	{
		Vector2 cellSize = new Vector2(minCellWidth, slotLayoutGroup.cellSize.y);
		SetCapacity(labels.Length);
		for (int i = 0; i < base.slots.Count; i++)
		{
			TextButton textButton = base.slots[i];
			textButton.text = labels[i];
			cellSize.x = Mathf.Max(cellSize.x, textButton.preferredWidth);
			textButton.transform.SetSiblingIndex(i);
		}
		slotLayoutGroup.cellSize = cellSize;
		leftButton.gameObject.SetActive(base.slotCount > 1);
		rightButton.gameObject.SetActive(base.slotCount > 1);
	}

	public override void BuildNavigation()
	{
		for (int i = 0; i < base.slots.Count; i++)
		{
			base.slots[i].button.SetNavigation();
		}
	}

	private void SelectPrev()
	{
		int index = (base.selectedIndex + base.slotCount - 1) % base.slotCount;
		Select(index);
	}

	private void SelectNext()
	{
		int index = (base.selectedIndex + 1) % base.slotCount;
		Select(index);
	}

	public void FireClickLeft()
	{
		if (base.slotCount > 1)
		{
			leftButton.FireClick();
		}
	}

	public void FireClickRight()
	{
		if (base.slotCount > 1)
		{
			rightButton.FireClick();
		}
	}
}
