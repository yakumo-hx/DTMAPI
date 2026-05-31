using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class DolocPagedGridUI<TSlot, TData> : DolocUIPanel, INavPanel, IPageUI<TData>, IView where TSlot : DolocNavigationButton where TData : IUIData
{
	[SerializeField]
	protected Text listTitle;

	[SerializeField]
	protected GridLayoutGroup slotsRoot;

	[SerializeField]
	protected Text emptyInfo;

	[SerializeField]
	protected PageInfo pageInfo;

	[HideInInspector]
	public UnityEvent<int> onDataSelect = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataClick = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataRightClick = new UnityEvent<int>();

	[HideInInspector]
	public Func<int, bool> onDataRightContinuesClick;

	[HideInInspector]
	public UnityEvent<int> onDataDeselect = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataPointerEnter = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataPointerExit = new UnityEvent<int>();

	protected int currentSlotIndex;

	public Selectable[] allSelectablesArray => ((IEnumerable<TSlot>)slots).Select((Func<TSlot, Selectable>)((TSlot x) => x.button)).ToArray();

	public int allSelectableCount => allSelectablesArray.Length;

	public TSlot[] slots { get; private set; }

	public Func<int, int, TData[]> DataGetter { get; set; }

	protected TData[] currentDatas { get; private set; }

	private int lastSlotIndex => RealSlotCountPerPage - 1;

	private int LineCapacity => (int)(slotsRoot.preferredWidth - (float)slotsRoot.padding.left - (float)slotsRoot.padding.right + slotsRoot.spacing.x) / (int)(slotsRoot.cellSize.x + slotsRoot.spacing.x);

	public bool WrapAround { get; set; } = true;


	public int TotalCapacity { get; private set; }

	public int TotalPageCount { get; private set; }

	public int SlotCountPerPage => slots.Length;

	public int currentPageIndex { get; private set; }

	protected int offset => currentPageIndex * SlotCountPerPage;

	public int selectedIndex => currentSlotIndex + offset;

	public int RealSlotCountPerPage { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		slots = GetSlots();
		InitSlots();
		pageInfo.Init();
		pageInfo.BindChangePageAction(PrevPage, NextPage);
	}

	protected abstract TSlot[] GetSlots();

	private void InitSlots()
	{
		if (slots.IsNullOrEmpty())
		{
			Debug.LogWarning("没有初始化slots");
		}
		int num = 0;
		TSlot[] array = slots;
		foreach (TSlot slot in array)
		{
			slot.Init();
			slot.index = num++;
			slot.onSelect.AddListener(delegate(int idx)
			{
				currentSlotIndex = idx;
				onDataSelect.Invoke(idx + offset);
			});
			slot.SetClickCallbacks(delegate(int idx)
			{
				onDataClick.Invoke(idx + offset);
			}, delegate(int idx)
			{
				onDataRightClick.Invoke(idx + offset);
			}, null, null, null, null, null, (int idx) => onDataRightContinuesClick?.Invoke(idx + offset) ?? false);
			slot.onDeselect.AddListener(delegate(int idx)
			{
				onDataDeselect.Invoke(idx + offset);
			});
			slot.button.onMove.AddListener(delegate(MoveDirection dir)
			{
				OnMove(slot.index, dir);
			});
			slot.onPointerEnter.AddListener(delegate(int idx)
			{
				onDataPointerEnter.Invoke(idx + offset);
			});
			slot.onPointerExit.AddListener(delegate(int idx)
			{
				onDataPointerExit.Invoke(idx + offset);
			});
			slot.visible = false;
			OnInitSlot(slot);
		}
	}

	protected abstract void OnInitSlot(TSlot slot);

	protected abstract void RenderSlot(TSlot slot, TData data);

	public void RefreshView()
	{
		currentDatas = null;
		if (DataGetter == null)
		{
			Debug.LogError($"{GetType()}未绑定DattaGetter!");
			return;
		}
		RealSlotCountPerPage = Mathf.Min(TotalCapacity - offset, SlotCountPerPage);
		currentDatas = DataGetter(offset, offset + RealSlotCountPerPage).ToArray();
		emptyInfo.gameObject.SetActive(currentDatas.Length == 0);
		if (currentDatas.Length < RealSlotCountPerPage)
		{
			Debug.LogWarning("数据缺失，请检查totalCapacity设置和DataGetter返回值是否正确");
			return;
		}
		for (int i = 0; i < RealSlotCountPerPage; i++)
		{
			TData val = currentDatas[i];
			TSlot val2 = slots[i];
			if (val != null && val.notEmpty)
			{
				val2.visible = true;
				RenderSlot(val2, val);
			}
			else
			{
				Debug.LogWarning($"ui数据index: {offset + i}为空");
				val2.visible = false;
			}
		}
		for (int j = RealSlotCountPerPage; j < SlotCountPerPage; j++)
		{
			slots[j].visible = false;
		}
		TSlot[] array = slots;
		foreach (TSlot val3 in array)
		{
			if (val3.index + offset >= TotalCapacity)
			{
				if (val3.index == 0 && TotalCapacity != 0)
				{
					Debug.LogError("错误的分页");
					break;
				}
				val3.visible = false;
			}
		}
		DolocAPI.Delay(0.02f, RebuildNavigation);
		pageInfo.SetPage(currentPageIndex, TotalPageCount);
		OnRefreshView();
	}

	private void RebuildNavigation()
	{
		if (TotalPageCount != 0)
		{
			for (int i = 0; i < RealSlotCountPerPage; i++)
			{
				Navigation navigation = default(Navigation);
				navigation.mode = Navigation.Mode.Explicit;
				DolocButtonComponent button = slots[i].button;
				int num = i - LineCapacity;
				navigation.selectOnUp = ((num < 0) ? null : slots[num].button);
				num = i + LineCapacity;
				navigation.selectOnDown = ((num > lastSlotIndex) ? null : slots[num].button);
				num = i - 1;
				navigation.selectOnLeft = ((num < 0) ? null : slots[num].button);
				num = i + 1;
				navigation.selectOnRight = ((num > lastSlotIndex) ? null : slots[num].button);
				button.navigation = navigation;
			}
		}
	}

	public void SetTotalCapacity(int totalCapacity)
	{
		if (TotalCapacity != totalCapacity)
		{
			TotalCapacity = totalCapacity;
			currentPageIndex = 0;
			TotalPageCount = Mathf.CeilToInt((float)totalCapacity / (float)SlotCountPerPage);
		}
	}

	private void FixPageChanged(int oldPageIndex, int newPageIndex)
	{
		if (oldPageIndex != newPageIndex)
		{
			RefreshView();
		}
		if (currentSlotIndex > lastSlotIndex)
		{
			slots[lastSlotIndex].Select();
			return;
		}
		EventSystem.current?.SetSelectedGameObject(null);
		slots[currentSlotIndex].Select();
	}

	public void PrevPage()
	{
		if (TotalPageCount != 0)
		{
			int oldPageIndex = currentPageIndex;
			currentPageIndex = (currentPageIndex + TotalPageCount - 1) % TotalPageCount;
			FixPageChanged(oldPageIndex, currentPageIndex);
		}
	}

	public void NextPage()
	{
		if (TotalPageCount != 0)
		{
			int oldPageIndex = currentPageIndex;
			currentPageIndex = (currentPageIndex + 1) % TotalPageCount;
			FixPageChanged(oldPageIndex, currentPageIndex);
		}
	}

	private void PrevPageAtLast()
	{
		if (TotalPageCount != 0 && (WrapAround || currentPageIndex != 0))
		{
			int num = currentPageIndex;
			currentPageIndex = (currentPageIndex + TotalPageCount - 1) % TotalPageCount;
			if (num != currentPageIndex)
			{
				RefreshView();
			}
			slots[lastSlotIndex].Select();
		}
	}

	private void PrevPageAtLastRow()
	{
		if (TotalPageCount != 0 && (WrapAround || currentPageIndex != 0))
		{
			int num = currentPageIndex;
			currentPageIndex = (currentPageIndex + TotalPageCount - 1) % TotalPageCount;
			if (num != currentPageIndex)
			{
				RefreshView();
			}
			int num2 = (Mathf.CeilToInt((float)RealSlotCountPerPage * 1f / (float)LineCapacity) - 1) * LineCapacity + currentSlotIndex;
			num2 = ((num2 > lastSlotIndex) ? lastSlotIndex : num2);
			slots[num2].Select();
		}
	}

	private void NextPageAtFirst()
	{
		if (TotalPageCount != 0 && (WrapAround || currentPageIndex != TotalPageCount - 1))
		{
			int num = currentPageIndex;
			currentPageIndex = (currentPageIndex + 1) % TotalPageCount;
			if (num != currentPageIndex)
			{
				RefreshView();
			}
			slots[0].Select();
		}
	}

	private void NextPageAtFirstRow()
	{
		if (TotalPageCount != 0 && (WrapAround || currentPageIndex != TotalPageCount - 1))
		{
			int num = currentPageIndex;
			currentPageIndex = (currentPageIndex + 1) % TotalPageCount;
			if (num != currentPageIndex)
			{
				RefreshView();
			}
			int num2 = currentSlotIndex % LineCapacity;
			int num3 = ((num2 > lastSlotIndex) ? lastSlotIndex : num2);
			slots[num3].Select();
		}
	}

	private void OnMove(int index, MoveDirection direction)
	{
		switch (direction)
		{
		case MoveDirection.Left:
			if (index == 0)
			{
				PrevPageAtLast();
			}
			break;
		case MoveDirection.Right:
			if (index == lastSlotIndex)
			{
				NextPageAtFirst();
			}
			break;
		case MoveDirection.Up:
			if (index == currentSlotIndex)
			{
				PrevPageAtLastRow();
			}
			break;
		case MoveDirection.Down:
			if (index == currentSlotIndex)
			{
				NextPageAtFirstRow();
			}
			break;
		}
	}

	public void Select(int index)
	{
		if (index >= 0 && index <= TotalCapacity)
		{
			int num = index / SlotCountPerPage;
			if (currentPageIndex != num)
			{
				currentPageIndex = num;
				RefreshView();
			}
			TSlot val = slots[index % SlotCountPerPage];
			EventSystem.current?.SetSelectedGameObject(null);
			val.Select();
		}
	}

	public void FireClickLeftButton()
	{
		pageInfo.FireClickLeft();
	}

	public void FireClickRightButton()
	{
		pageInfo.FireClickRight();
	}

	public void FireClickCloseButton()
	{
		closeButton.FireClick();
	}

	public void GetFocus()
	{
		if (currentSlotIndex < RealSlotCountPerPage)
		{
			slots[currentSlotIndex].Select();
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		RefreshView();
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		RebuildNavigation();
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		EventSystem.current?.SetSelectedGameObject(null);
		TotalCapacity = 0;
		TotalPageCount = 0;
		currentPageIndex = 0;
		currentDatas = null;
	}

	protected virtual void OnRefreshView()
	{
	}

	public void SetTitle(string title)
	{
		if (!(listTitle == null))
		{
			listTitle.text = title;
		}
	}

	public TSlot GetSlot(int index)
	{
		index %= SlotCountPerPage;
		if (index < 0 || index >= slots.Length)
		{
			return null;
		}
		return slots[index];
	}

	public void SetEmptyInfo(string info)
	{
		emptyInfo.text = info;
	}

	private void RebuildNavigationInCol()
	{
		if (TotalPageCount != 0)
		{
			int constraintCount = slotsRoot.constraintCount;
			for (int i = 0; i < RealSlotCountPerPage; i++)
			{
				Navigation navigation = default(Navigation);
				navigation.mode = Navigation.Mode.Explicit;
				DolocButtonComponent button = slots[i].button;
				int num = i - 1;
				navigation.selectOnUp = ((num < 0) ? null : slots[num].button);
				num = i + 1;
				navigation.selectOnDown = ((num > lastSlotIndex) ? null : slots[num].button);
				num = i - constraintCount;
				navigation.selectOnLeft = ((num < 0) ? null : slots[num].button);
				num = i + constraintCount;
				navigation.selectOnRight = ((num > lastSlotIndex) ? null : slots[num].button);
				button.navigation = navigation;
			}
		}
	}

	private void OnMoveInCol(int index, MoveDirection direction)
	{
		switch (direction)
		{
		case MoveDirection.Left:
			if (index == currentSlotIndex)
			{
				PrevPageAtLastCol();
			}
			break;
		case MoveDirection.Right:
			if (index == currentSlotIndex)
			{
				NextPageAtFirstCol();
			}
			break;
		case MoveDirection.Up:
			if (index == 0)
			{
				PrevPageAtLast();
			}
			break;
		case MoveDirection.Down:
			if (index == lastSlotIndex)
			{
				NextPageAtFirst();
			}
			break;
		}
	}

	private void PrevPageAtLastCol()
	{
		if (TotalPageCount != 0 && (WrapAround || currentPageIndex != 0))
		{
			int num = currentPageIndex;
			currentPageIndex = (currentPageIndex + TotalPageCount - 1) % TotalPageCount;
			if (num != currentPageIndex)
			{
				RefreshView();
			}
			int constraintCount = slotsRoot.constraintCount;
			int num2 = (Mathf.CeilToInt((float)RealSlotCountPerPage * 1f / (float)constraintCount) - 1) * constraintCount + currentSlotIndex;
			num2 = ((num2 > lastSlotIndex) ? lastSlotIndex : num2);
			slots[num2].Select();
		}
	}

	private void NextPageAtFirstCol()
	{
		if (TotalPageCount != 0 && (WrapAround || currentPageIndex != TotalPageCount - 1))
		{
			int num = currentPageIndex;
			currentPageIndex = (currentPageIndex + 1) % TotalPageCount;
			if (num != currentPageIndex)
			{
				RefreshView();
			}
			int constraintCount = slotsRoot.constraintCount;
			int num2 = currentSlotIndex % constraintCount;
			int num3 = ((num2 > lastSlotIndex) ? lastSlotIndex : num2);
			slots[num3].Select();
		}
	}
}
