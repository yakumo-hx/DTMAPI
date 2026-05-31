using System;
using System.Collections.Generic;
using DolocTown.Config.Recipe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DishPanel : DolocUIPanel, INavPanel
{
	[SerializeField]
	private DishViewer dishViewer;

	[SerializeField]
	private Text listTitle;

	[SerializeField]
	private Transform slotRoot;

	[SerializeField]
	private CanvasGroup slotsCanvasGroup;

	[SerializeField]
	public ConfirmCraftButton confirmButton;

	[SerializeField]
	public SwitchBar switchBar;

	[HideInInspector]
	public UnityEvent<int> OnSlotClick = new UnityEvent<int>();

	private DishSlot[] slots;

	private bool inTask;

	public bool UseCustomHiddenPosition;

	public Vector2 HiddenPosition;

	protected override bool useDefaultHidePosition => UseCustomHiddenPosition;

	public Selectable[] allSelectablesArray
	{
		get
		{
			if (inTask)
			{
				return Array.Empty<Selectable>();
			}
			List<Selectable> list = new List<Selectable>();
			DishSlot[] array = slots;
			foreach (DishSlot dishSlot in array)
			{
				if (dishSlot.visible)
				{
					list.Add(dishSlot.button);
				}
			}
			if (confirmButton.visible)
			{
				list.Add(confirmButton.button);
			}
			return list.ToArray();
		}
	}

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		dishViewer.Init();
		confirmButton.Init();
		switchBar.Init();
		slots = base.transform.GetComponentsInChildren<DishSlot>();
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i].Init();
			slots[i].index = i;
			slots[i].onSelect.AddListener(delegate
			{
				DolocAPI.HideItemBorder();
			});
			slots[i].onClick.AddListener(OnSlotClick.Invoke);
		}
	}

	public void SetCapacity(int slotCount)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i].SetVisible(i < slotCount);
		}
	}

	public void RenderLockedViewer(DishGroupInfo groupProto)
	{
		listTitle.text = base.staticTexts.RecipePanelMaterialList;
		dishViewer.RenderLocked(groupProto);
		confirmButton.buttonText = DolocUtils.Format(base.staticTexts.RecipePanelConfirmWithTime, "???");
		confirmButton.grayed = true;
	}

	public void RenderViewer(RecipeData data)
	{
		listTitle.text = base.staticTexts.RecipePanelMaterialList;
		dishViewer.Render(data);
		confirmButton.buttonText = DolocUtils.Format(base.staticTexts.RecipePanelConfirmWithTime, DolocAPI.GetFormatTimeLengthByTU(data.timeUnit));
		confirmButton.grayed = true;
	}

	public void SetTaskInfo(string text)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		inTask = !text.IsNullOrEmpty();
		dishViewer.SetTaskInfo(text);
		confirmButton.gameObject.SetActive(!inTask);
		slotsCanvasGroup.alpha = (inTask ? 0.3f : 1f);
		DishSlot[] array = slots;
		foreach (DishSlot dishSlot in array)
		{
			if (inTask && dishSlot.isEmpty)
			{
				dishSlot.SetVisible(value: false);
			}
		}
	}

	public void OnDishSlotChange(int index, Item item)
	{
		GetSlot(index).Render(item);
	}

	private DishSlot GetSlot(int index)
	{
		return slots[Mathf.Clamp(index, 0, slots.Length - 1)];
	}

	protected override Vector2 GetCustomHidePosition(bool visible, UiPanelDisplayAnimType type)
	{
		if (useDefaultHidePosition)
		{
			return HiddenPosition;
		}
		return GetAnchoredHidePosition(visible, type);
	}

	public void ClearSlots()
	{
		DishSlot[] array = slots;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Clear();
		}
	}
}
