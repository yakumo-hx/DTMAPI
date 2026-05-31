using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class DolocScrollGridUI<TSlot, TData> : DolocGridUI<TSlot>, INavPanel where TSlot : DolocNavigationButton where TData : IUIData
{
	[SerializeField]
	public ScrollRect _scrollRect;

	[SerializeField]
	protected RectTransform contentRect;

	public virtual Selectable[] allSelectablesArray => ((IEnumerable<TSlot>)slotPool).Select((Func<TSlot, Selectable>)((TSlot x) => x.button)).ToArray();

	public int allSelectableCount => allSelectablesArray.Length;

	protected TData[] currentDatas { get; private set; }

	protected abstract void RenderSlot(TSlot slot, TData data);

	public void RefreshView(TData[] datas)
	{
		currentDatas = datas;
		int num = ((datas != null) ? datas.Length : 0);
		CheckCount(num);
		for (int i = 0; i < num; i++)
		{
			TData val = datas[i];
			TSlot val2 = slotPool[i];
			if (val != null && val.notEmpty)
			{
				val2.visible = true;
				RenderSlot(val2, val);
			}
			else
			{
				val2.visible = false;
			}
		}
		SetCapacity(num, slotLayoutGroup.constraintCount);
		OnRefreshView();
	}

	protected virtual void OnRefreshView()
	{
	}

	public void ResetNormalizedPosition()
	{
		_scrollRect.horizontalNormalizedPosition = 0f;
		_scrollRect.verticalNormalizedPosition = 1f;
	}

	public override void SetCapacity(int totalCapacity, int lineCapacity)
	{
		base.totalCapacity = totalCapacity;
		this.lineCapacity = lineCapacity;
		base.rowCount = Mathf.CeilToInt((float)totalCapacity / (float)lineCapacity);
		slotLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		slotLayoutGroup.constraintCount = lineCapacity;
		SetClickCallbacks(clickCallback);
		SetSelectCallbacks(selectCallback);
		SetDeselectCallbacks(deselectCallback);
		SetPointerEnterCallbacks(pointerEnterCallback);
		SetPointerExitCallbacks(pointerExitCallback);
		BuildNavigation();
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		EventSystem.current?.SetSelectedGameObject(null);
		currentDatas = null;
	}

	protected bool TryGetData(int index, out TData data)
	{
		data = default(TData);
		if (currentDatas == null)
		{
			return false;
		}
		if (index < 0 || index >= currentDatas.Length)
		{
			return false;
		}
		data = currentDatas[index];
		return true;
	}

	protected override void OnSlotSelect(TSlot slot)
	{
		base.OnSlotSelect(slot);
		Vector3[] worldCorners = GetWorldCorners(_scrollRect.viewport);
		Vector2 vector = worldCorners[0];
		Vector2 vector2 = worldCorners[2];
		Vector3[] worldCorners2 = GetWorldCorners(slot.rectTransform);
		if (worldCorners2[0].y < vector.y)
		{
			_scrollRect.content.position += new Vector3(0f, vector.y - worldCorners2[0].y);
		}
		else if (worldCorners2[2].y > vector2.y)
		{
			_scrollRect.content.position += new Vector3(0f, vector2.y - worldCorners2[2].y);
		}
	}

	private void MoveAndLocation(int index)
	{
		float num = _scrollRect.GetComponent<RectTransform>().rect.height;
		float num2 = contentRect.rect.height;
		RectTransform component = base.slots[index].transform.GetComponent<RectTransform>();
		float num3 = component.anchoredPosition.y - component.rect.height;
		_scrollRect.verticalNormalizedPosition = Mathf.Clamp01((num3 + num2) / (num2 - num));
	}

	private Vector3[] GetWorldCorners(RectTransform source)
	{
		Vector3[] array = new Vector3[4];
		source.GetWorldCorners(array);
		return array;
	}
}
