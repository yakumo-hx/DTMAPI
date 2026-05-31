using System;
using System.Collections.Generic;
using RedSaw;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class DolocGridUI<T> : AutoSizeUIPanel where T : DolocNavigationButton
{
	protected UnityAction<int> clickCallback;

	protected UnityAction<int> selectCallback;

	protected UnityAction<int> deselectCallback;

	protected UnityAction<int> pointerEnterCallback;

	protected UnityAction<int> pointerExitCallback;

	[SerializeField]
	protected GridLayoutGroup slotLayoutGroup;

	protected ObjectPool<T> slotPool;

	private int _selectedIndex;

	public bool isFocused { get; protected set; }

	protected virtual LayoutType layoutType => LayoutType.Grid;

	protected Button currentButton
	{
		get
		{
			if (slotCount <= 0 || slotCount <= selectedIndex)
			{
				return null;
			}
			return GetSlot(selectedIndex).button;
		}
	}

	protected int totalCapacity { get; set; }

	protected virtual int lineCapacity { get; set; }

	public int rowCount { get; protected set; }

	protected virtual bool disableRecycleOnInit => false;

	protected int slotCount
	{
		get
		{
			if (slotPool == null)
			{
				return 0;
			}
			return slotPool.ActiveCount;
		}
	}

	public List<T> slots => slotPool?.Instances;

	public Vector2 CellSize => slotLayoutGroup.cellSize;

	public Vector2 Spacing => slotLayoutGroup.spacing;

	public int selectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			if (_selectedIndex != value)
			{
				OnSelectedIndexChange(_selectedIndex, value);
			}
			_selectedIndex = value;
		}
	}

	protected abstract GameObject slotPrefab { get; }

	public virtual void SetCapacity(int totalCapacity, int lineCapacity)
	{
		if (this.totalCapacity != totalCapacity || this.lineCapacity != lineCapacity)
		{
			this.totalCapacity = totalCapacity;
			this.lineCapacity = lineCapacity;
			rowCount = Mathf.CeilToInt((float)totalCapacity / (float)lineCapacity);
			slotLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			slotLayoutGroup.constraintCount = lineCapacity;
			CheckCount(totalCapacity);
			RebuildLayout();
			SetClickCallbacks(clickCallback);
			SetSelectCallbacks(selectCallback);
			SetDeselectCallbacks(deselectCallback);
			SetPointerEnterCallbacks(pointerEnterCallback);
			SetPointerExitCallbacks(pointerExitCallback);
			BuildNavigation();
		}
	}

	protected void CheckCount(int count)
	{
		slotPool.CheckCount(count);
		slotPool.Sort();
	}

	public void ResetLayoutSize(int count)
	{
		slotLayoutGroup.constraintCount = count;
		RebuildLayout();
	}

	public virtual void GetFocus()
	{
		isFocused = true;
		if (currentButton != null)
		{
			EventSystem.current?.SetSelectedGameObject(null);
			currentButton.Select();
		}
	}

	public virtual void LoseFocus()
	{
		isFocused = false;
	}

	public virtual void SetClickCallbacks(UnityAction<int> callback)
	{
		clickCallback = callback;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			T val = slots[i];
			val.index = i;
			ResetSlotClickCallback(val);
			if (callback != null)
			{
				val.onClick.AddListener(callback);
			}
		}
	}

	public virtual void SetClickCallbacks(UnityAction<int> onLeftClick, UnityAction<int> onRightClick = null, UnityAction<int> onAssistLeftClick = null, UnityAction<int> onAssistRightClick = null, UnityAction<int> onLeftLongClick = null, UnityAction<int> onRightLongClick = null, Func<int, bool> onLeftContinuesClick = null, Func<int, bool> onRightContinuesClick = null)
	{
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			T val = slots[i];
			val.index = i;
			val.SetClickCallbacks(onLeftClick, onRightClick, onAssistLeftClick, onAssistRightClick, onLeftLongClick, onRightLongClick, onLeftContinuesClick, onRightContinuesClick);
		}
	}

	private void ResetSlotClickCallback(T slot)
	{
		slot.onClick.RemoveAllListeners();
		slot.onClick.AddListener(delegate(int idx)
		{
			selectedIndex = idx;
		});
		slot.onClick.AddListener(delegate
		{
			OnSlotClick(slot);
		});
	}

	protected virtual void OnSlotClick(T slot)
	{
	}

	public virtual void SetSelectCallbacks(UnityAction<int> callback)
	{
		selectCallback = callback;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			T val = slots[i];
			val.index = i;
			ResetSlotSelectCallback(val);
			if (callback != null)
			{
				val.onSelect.AddListener(callback);
			}
		}
	}

	protected void ResetSlotSelectCallback(T slot)
	{
		slot.onSelect.RemoveAllListeners();
		slot.onSelect.AddListener(delegate(int idx)
		{
			selectedIndex = idx;
		});
		slot.onSelect.AddListener(delegate
		{
			isFocused = true;
		});
		slot.onSelect.AddListener(delegate
		{
			OnSlotSelect(slot);
		});
	}

	protected virtual void OnSlotSelect(T slot)
	{
	}

	public virtual void SetDeselectCallbacks(UnityAction<int> callback)
	{
		deselectCallback = callback;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			T val = slots[i];
			val.index = i;
			ResetSlotDeselectCallback(val);
			if (callback != null)
			{
				val.onDeselect.AddListener(callback);
			}
		}
	}

	protected void ResetSlotDeselectCallback(T slot)
	{
		slot.onDeselect.RemoveAllListeners();
		slot.onDeselect.AddListener(delegate
		{
			OnSlotDeselect(slot);
		});
		slot.onDeselect.AddListener(delegate
		{
			isFocused = false;
		});
	}

	protected virtual void OnSlotDeselect(T slot)
	{
	}

	public virtual void SetPointerEnterCallbacks(UnityAction<int> callback)
	{
		pointerEnterCallback = callback;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			T val = slots[i];
			val.index = i;
			ResetSlotPointerEnterCallback(val);
			if (callback != null)
			{
				val.onPointerEnter.AddListener(callback);
			}
		}
	}

	protected void ResetSlotPointerEnterCallback(T slot)
	{
		slot.onPointerEnter.RemoveAllListeners();
		slot.onPointerEnter.AddListener(delegate
		{
			OnSlotPointerEnter(slot);
		});
	}

	protected virtual void OnSlotPointerEnter(T slot)
	{
	}

	public virtual void SetPointerExitCallbacks(UnityAction<int> callback)
	{
		pointerExitCallback = callback;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			T val = slots[i];
			val.index = i;
			ResetSlotPointerExitCallback(val);
			if (callback != null)
			{
				val.onPointerExit.AddListener(callback);
			}
		}
	}

	protected void ResetSlotPointerExitCallback(T slot)
	{
		slot.onPointerExit.RemoveAllListeners();
		slot.onPointerExit.AddListener(delegate
		{
			OnSlotPointerExit(slot);
		});
	}

	protected virtual void OnSlotPointerExit(T slot)
	{
	}

	public virtual void RemoveCallbacks()
	{
		clickCallback = null;
		selectCallback = null;
		deselectCallback = null;
		pointerEnterCallback = null;
		pointerExitCallback = null;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			T slot = slots[i];
			slots[i].ClearAllClickCallbacks();
			ResetSlotClickCallback(slot);
			ResetSlotSelectCallback(slot);
			ResetSlotDeselectCallback(slot);
			ResetSlotPointerEnterCallback(slot);
			ResetSlotPointerExitCallback(slot);
		}
	}

	public virtual void BuildNavigation()
	{
		GridSelector gridSelector = new GridSelector();
		gridSelector.Init(totalCapacity, lineCapacity, null);
		for (int i = 0; i < totalCapacity; i++)
		{
			Navigation navigation = default(Navigation);
			navigation.mode = Navigation.Mode.Explicit;
			DolocButtonComponent button = slots[i].button;
			if (layoutType == LayoutType.Horizontal)
			{
				navigation.selectOnUp = null;
				navigation.selectOnDown = null;
			}
			else
			{
				gridSelector.SetSelectionIndex(i);
				gridSelector.MoveUp();
				navigation.selectOnUp = ((i == gridSelector.currentIndex) ? null : slots[gridSelector.currentIndex].button);
				gridSelector.SetSelectionIndex(i);
				gridSelector.MoveDown();
				navigation.selectOnDown = ((i == gridSelector.currentIndex) ? null : slots[gridSelector.currentIndex].button);
			}
			if (layoutType == LayoutType.Vertical)
			{
				navigation.selectOnLeft = null;
				navigation.selectOnRight = null;
			}
			else
			{
				gridSelector.SetSelectionIndex(i);
				gridSelector.MoveLeft();
				navigation.selectOnLeft = ((i == gridSelector.currentIndex) ? null : slots[gridSelector.currentIndex].button);
				gridSelector.SetSelectionIndex(i);
				gridSelector.MoveRight();
				navigation.selectOnRight = ((i == gridSelector.currentIndex) ? null : slots[gridSelector.currentIndex].button);
			}
			button.navigation = navigation;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		if (slotLayoutGroup == null)
		{
			slotLayoutGroup = GetComponentInChildren<GridLayoutGroup>(includeInactive: true);
		}
		slotPool = new ObjectPool<T>(slotPrefab, slotLayoutGroup.transform, usePreset: true, disableRecycleOnInit);
		ObjectPool<T> objectPool = slotPool;
		objectPool.OnCreate = (Action<T>)Delegate.Combine(objectPool.OnCreate, new Action<T>(OnSlotCreate));
	}

	public virtual void Select(int index)
	{
		if (slotCount != 0)
		{
			T slot = GetSlot(index);
			if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == slot.gameObject)
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
			slot.Select();
		}
	}

	public virtual void SelectFirst()
	{
		Select(0);
	}

	public virtual void SelectLast()
	{
		Select(slotCount - 1);
	}

	public void FireClick(int index, bool fireSelect = false, bool ignoreActiveState = true)
	{
		GetSlot(index).FireClick(fireSelect, ignoreActiveState);
	}

	public virtual T GetSlot(int index)
	{
		if (slots.Count == 0)
		{
			return null;
		}
		return slots[Mathf.Clamp(index, 0, slotCount - 1)];
	}

	protected virtual void OnSelectedIndexChange(int oldValue, int newValue)
	{
	}

	protected virtual void OnSlotCreate(T slot)
	{
	}
}
