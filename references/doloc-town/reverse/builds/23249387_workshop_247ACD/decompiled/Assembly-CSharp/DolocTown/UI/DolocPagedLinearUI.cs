using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class DolocPagedLinearUI<TSlot, TData> : AutoSizeUIPanel, INavPanel, IPageUI<TData>, IView where TSlot : DolocNavigationButton where TData : IUIData
{
	[SerializeField]
	protected Text listTitle;

	[SerializeField]
	protected Transform slotsRoot;

	[SerializeField]
	protected Text emptyInfo;

	[SerializeField]
	protected PageInfo pageInfo;

	[HideInInspector]
	public UnityEvent<int> onDataSelect = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataClick = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataAssistClick = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataRightClick = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataLongClick = new UnityEvent<int>();

	[HideInInspector]
	public Func<int, bool> onDataRightContinuesClick;

	[HideInInspector]
	public UnityEvent<int> onDataDeselect = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataPointerEnter = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<int> onDataPointerExit = new UnityEvent<int>();

	private int lastSlotIndex;

	protected int currentSlotIndex;

	protected virtual bool clearOtherDirectionNav => true;

	public Selectable[] allSelectablesArray => ((IEnumerable<TSlot>)slots).Select((Func<TSlot, Selectable>)((TSlot x) => x.button)).ToArray();

	public int allSelectableCount => allSelectablesArray.Length;

	public TSlot[] slots { get; private set; }

	public Func<int, int, TData[]> DataGetter { get; set; }

	protected TData[] currentDatas { get; private set; }

	protected abstract SlotLayout layout { get; }

	public int totalCapacity { get; private set; }

	public int totalPageCount { get; private set; }

	public int slotCountPerPage => slots.Length;

	public int currentPageIndex { get; private set; }

	public int offset => currentPageIndex * slotCountPerPage;

	public int selectedIndex => currentSlotIndex + offset;

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
			}, delegate(int idx)
			{
				onDataAssistClick.Invoke(idx + offset);
			}, null, delegate(int idx)
			{
				onDataLongClick.Invoke(idx + offset);
			}, null, null, (int idx) => onDataRightContinuesClick?.Invoke(idx + offset) ?? false);
			slot.onDeselect.AddListener(delegate(int idx)
			{
				onDataDeselect.Invoke(idx + offset);
			});
			slot.onPointerEnter.AddListener(delegate(int idx)
			{
				onDataPointerEnter.Invoke(idx + offset);
			});
			slot.onPointerExit.AddListener(delegate(int idx)
			{
				onDataPointerExit.Invoke(idx + offset);
			});
			slot.button.onMove.AddListener(delegate(MoveDirection dir)
			{
				OnMove(slot.index, dir);
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
		int num = Mathf.Min(totalCapacity - offset, slotCountPerPage);
		lastSlotIndex = num - 1;
		try
		{
			currentDatas = DataGetter(offset, offset + num).ToArray();
		}
		catch (Exception ex)
		{
			currentDatas = Array.Empty<TData>();
			Debug.LogError("<" + GetType().Name + ">获取ui数据失败");
			Debug.LogException(ex);
			Debug.LogError(ex.Message);
		}
		if (currentDatas.Length == 0 && totalCapacity > 0)
		{
			PrevPage();
			return;
		}
		emptyInfo.gameObject.SetActive(currentDatas.Length == 0);
		if (currentDatas.Length < num)
		{
			Debug.LogWarning("数据缺失，请检查totalCapacity设置和DataGetter返回值是否正确");
			return;
		}
		for (int i = 0; i < num; i++)
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
		for (int j = num; j < slotCountPerPage; j++)
		{
			if (num < slots.Length && num >= 0)
			{
				slots[j].visible = false;
			}
		}
		TSlot[] array = slots;
		foreach (TSlot val3 in array)
		{
			if (val3.index + offset >= totalCapacity)
			{
				if (val3.index == 0 && totalCapacity != 0)
				{
					Debug.LogError("错误的分页");
					break;
				}
				val3.visible = false;
			}
		}
		if (currentSlotIndex >= num && num > 0)
		{
			slots[num - 1].Select();
		}
		DolocAPI.Delay(0.02f, RebuildNavigation);
		pageInfo.SetPage(currentPageIndex, totalPageCount);
		OnRefreshView();
	}

	private void RebuildNavigation()
	{
		if (totalPageCount != 0)
		{
			if (layout == SlotLayout.Horizontal)
			{
				this.RebuildNavigationHorizontal(allSelectablesArray, 1f, 90f, wrapAround: false, clearOtherDirectionNav);
			}
			else
			{
				this.RebuildNavigationVertical(allSelectablesArray, 1f, 90f, wrapAround: false, clearOtherDirectionNav);
			}
		}
	}

	public void SetTotalCapacity(int totalCapacity)
	{
		if (this.totalCapacity != totalCapacity)
		{
			this.totalCapacity = totalCapacity;
			totalPageCount = Mathf.CeilToInt((float)totalCapacity / (float)slotCountPerPage);
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
		if (totalPageCount != 0)
		{
			int oldPageIndex = currentPageIndex;
			currentPageIndex = (currentPageIndex + totalPageCount - 1) % totalPageCount;
			FixPageChanged(oldPageIndex, currentPageIndex);
		}
	}

	public void NextPage()
	{
		if (totalPageCount != 0)
		{
			int oldPageIndex = currentPageIndex;
			currentPageIndex = (currentPageIndex + 1) % totalPageCount;
			FixPageChanged(oldPageIndex, currentPageIndex);
		}
	}

	private void PrevPageAtLast()
	{
		if (totalPageCount != 0)
		{
			int num = currentPageIndex;
			currentPageIndex = (currentPageIndex + totalPageCount - 1) % totalPageCount;
			if (num != currentPageIndex)
			{
				RefreshView();
			}
			EventSystem.current?.SetSelectedGameObject(null);
			slots[lastSlotIndex].Select();
		}
	}

	private void NextPageAtFirst()
	{
		if (totalPageCount != 0)
		{
			int num = currentPageIndex;
			currentPageIndex = (currentPageIndex + 1) % totalPageCount;
			if (num != currentPageIndex)
			{
				RefreshView();
			}
			EventSystem.current?.SetSelectedGameObject(null);
			slots[0].Select();
		}
	}

	private void OnMove(int index, MoveDirection direction)
	{
		if (index != 0 && index != lastSlotIndex)
		{
			return;
		}
		if (layout == SlotLayout.Vertical)
		{
			if (index == 0 && direction == MoveDirection.Up)
			{
				PrevPageAtLast();
			}
			else if (index == lastSlotIndex && direction == MoveDirection.Down)
			{
				NextPageAtFirst();
			}
		}
		else if (index == 0 && direction == MoveDirection.Left)
		{
			PrevPageAtLast();
		}
		else if (index == lastSlotIndex && direction == MoveDirection.Right)
		{
			NextPageAtFirst();
		}
	}

	public void Select(int index)
	{
		if (index >= 0 && index <= totalCapacity)
		{
			int num = index / slotCountPerPage;
			if (currentPageIndex != num)
			{
				currentPageIndex = num;
				RefreshView();
			}
			TSlot val = slots[index % slotCountPerPage];
			EventSystem.current?.SetSelectedGameObject(null);
			val.Select();
			DolocAPI.UIRaiseRoll();
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
		index %= slotCountPerPage;
		if (index < 0 || index >= slots.Length)
		{
			return null;
		}
		return slots[index];
	}

	public void GetFocus()
	{
		if (currentSlotIndex < slotCountPerPage)
		{
			slots[currentSlotIndex].Select();
		}
	}

	protected bool TryGetData(int index, out TData data)
	{
		index %= slotCountPerPage;
		if (index < 0 || index >= currentDatas.Length)
		{
			data = default(TData);
			return false;
		}
		data = currentDatas[index];
		return true;
	}

	public void RaiseSpriteFadeUp(int index)
	{
		TSlot slot = GetSlot(index);
		if (slot != null)
		{
			slot.RaiseUiSpriteFadeUp();
		}
	}

	public void RaiseSpriteFadeDown(int index)
	{
		TSlot slot = GetSlot(index);
		if (slot != null)
		{
			slot.RaiseUiSpriteFadeDown();
		}
	}

	public void SetEmptyInfo(string info)
	{
		emptyInfo.text = info;
	}
}
