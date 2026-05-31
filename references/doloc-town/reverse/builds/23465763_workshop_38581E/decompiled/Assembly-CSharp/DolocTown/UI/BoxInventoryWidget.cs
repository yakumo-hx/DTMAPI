using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BoxInventoryWidget : InventoryPanel
{
	public ItemNavSlot titleIcon;

	[SerializeField]
	private CanvasGroup titleCanvasGroup;

	[SerializeField]
	private CanvasGroup contentCanvasGroup;

	[HideInInspector]
	public UnityEvent onTitleIconSelect = new UnityEvent();

	public bool isEmpty => currentItemBox == null;

	public ItemBox currentItemBox { get; private set; }

	public new Selectable[] allSelectablesArray
	{
		get
		{
			Selectable[] array = new Selectable[base.totalCapacity + 1];
			int num = 0;
			array[num++] = titleIcon.button;
			foreach (ItemNavSlot slot in base.slots)
			{
				array[num++] = slot.button;
			}
			return array;
		}
	}

	public new int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		if (titleIcon == null)
		{
			titleIcon = GetComponentInChildren<ItemNavSlot>();
		}
		titleIcon.Init();
		titleIcon.index = -1;
		titleIcon.grayMode = ItemNavSlot.GrayMode.All;
		titleIcon.onPointerEnter.AddListener(delegate
		{
			titleIcon.HoverItemViewer(new ItemData(currentItemBox));
		});
		titleIcon.onPointerExit.AddListener(delegate
		{
			titleIcon.HideHoverBox();
		});
		titleIcon.onSelect.AddListener(delegate
		{
			onTitleIconSelect.Invoke();
			titleIcon.GetItemBorder();
			titleIcon.HoverItemViewer(new ItemData(currentItemBox));
		});
		titleIcon.onDeselect.AddListener(delegate
		{
			titleIcon.HideHoverBox();
		});
	}

	protected override int GetLineCapacity(int total)
	{
		return currentItemBox.inventory.capacity;
	}

	public void SetTitleInteractive(bool value)
	{
		titleCanvasGroup.interactable = value;
		titleCanvasGroup.blocksRaycasts = value;
		titleCanvasGroup.alpha = (value ? 1f : 0.5f);
	}

	public void SetContentInteractive(bool value)
	{
		contentCanvasGroup.interactable = value;
		contentCanvasGroup.blocksRaycasts = value;
		contentCanvasGroup.alpha = (value ? 1f : 0.5f);
	}

	public void BindBox(ItemBox box, Action<int, Item> onSlotRender = null)
	{
		currentItemBox = box;
		BindInventory(box.inventory, onSlotRender);
	}

	public void UnBindBox()
	{
		currentItemBox = null;
		titleIcon.Clear();
		UnBindInventory();
		foreach (ItemNavSlot slot in base.slots)
		{
			slot.grayed = false;
			slot.highLighted = false;
			slot.Clear();
		}
	}

	protected override void Render(int index, Item item, bool isSlotLocked)
	{
		base.Render(index, item, isSlotLocked);
		titleIcon.Render(currentItemBox);
	}

	public override ItemNavSlot GetSlot(int index)
	{
		if (index < 0)
		{
			return titleIcon;
		}
		return base.GetSlot(index);
	}

	public override void BuildNavigation()
	{
	}
}
