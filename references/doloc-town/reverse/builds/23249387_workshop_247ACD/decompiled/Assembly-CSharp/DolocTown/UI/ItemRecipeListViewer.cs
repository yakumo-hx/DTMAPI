using System;
using System.Collections.Generic;
using DG.Tweening;
using DolocTown.Config.Recipe;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ItemRecipeListViewer : DolocScrollGridUI<ItemRecipeSlot, ItemRecipeData>
{
	[SerializeField]
	public float distanceWeight = 1f;

	[SerializeField]
	public float angleLimit = 90f;

	[SerializeField]
	private RectTransform viewport;

	[SerializeField]
	private float moveDuration = 0.15f;

	[SerializeField]
	private float scrollOffset = 12f;

	[SerializeField]
	private ItemListViewer itemListViewer;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_MAKE_RECIPE_SLOT);

	public float SlotHeight => slotLayoutGroup.cellSize.y + slotLayoutGroup.spacing.y;

	public override Selectable[] allSelectablesArray
	{
		get
		{
			List<Selectable> list = new List<Selectable>();
			foreach (ItemRecipeSlot slot in base.slots)
			{
				list.AddRange(slot.allSelectablesArray);
			}
			return list.ToArray();
		}
	}

	protected override void RenderSlot(ItemRecipeSlot slot, ItemRecipeData data)
	{
		slot.Render(data);
	}

	protected override void __Init()
	{
		base.__Init();
		slotPool.RecycleAll();
		slotPool.OnReuse = delegate(ItemRecipeSlot slot)
		{
			slot.itemGetter = GetItemProto;
		};
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		DolocAPI.DelayFrame(BuildNavigation);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		SetSelectCallback(null);
	}

	protected override void OnRefreshView()
	{
		base.OnRefreshView();
		SetSelectCallback(itemListViewer.LoseFocus);
		DolocAPI.DelayFrame(BuildNavigation);
	}

	public override void BuildNavigation()
	{
		foreach (ItemRecipeSlot slot in base.slots)
		{
			slot.SetResetScrollRectCallback(delegate(RectTransform trans)
			{
				ResetRect(trans, slot.index == 0);
			});
		}
		foreach (ItemRecipeSlot slot2 in base.slots)
		{
			slot2.RebuildNavigationHorizontal();
		}
		this.RebuildNavigationVertical(allSelectablesArray, distanceWeight, angleLimit);
		int i;
		for (i = 0; i < base.slots.Count; i++)
		{
			if (base.slots[i].interactable)
			{
				int index = ((i - 1 < 0) ? (base.slots.Count - 1) : (i - 1));
				base.slots[index].allSelectablesArray.ForEach(delegate(Selectable select)
				{
					select.SetNavigationOnDown(base.slots[i].button);
				});
				base.slots[i].button.SetNavigationOnUp(base.slots[index].firstSelected);
				int index2 = ((i + 1 < base.slots.Count) ? (i + 1) : 0);
				base.slots[index2].allSelectablesArray.ForEach(delegate(Selectable select)
				{
					select.SetNavigationOnUp(base.slots[i].button);
				});
				base.slots[i].button.SetNavigationOnDown(base.slots[index2].firstSelected);
			}
		}
	}

	public override void SelectFirst()
	{
		if (base.slotCount != 0)
		{
			int index = (int)(_scrollRect.content.anchoredPosition.y / SlotHeight);
			ItemRecipeSlot slot = GetSlot(index);
			if (EventSystem.current?.currentSelectedGameObject == slot.firstSelected.gameObject)
			{
				EventSystem.current?.SetSelectedGameObject(null);
			}
			slot.firstSelected.Select();
		}
	}

	private void SetSelectCallback(Action callback)
	{
		foreach (ItemRecipeSlot slot in base.slots)
		{
			slot.SetSelectCallback(callback);
		}
	}

	private RecipeInfo GetItemProto(int index)
	{
		if (!TryGetData(index, out var data))
		{
			return null;
		}
		return data.recipeProto;
	}

	private void ResetRect(RectTransform rectTrans, bool isFirst)
	{
		if (base.slotCount == 0)
		{
			return;
		}
		if (isFirst)
		{
			contentRect.DOAnchorPosY(0f, moveDuration);
			return;
		}
		float num = viewport.rect.height;
		Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, rectTrans);
		float num2 = 0f;
		if (bounds.max.y + num > num)
		{
			num2 = -1f * (bounds.max.y + scrollOffset);
		}
		if (bounds.min.y + num < 0f)
		{
			num2 = -1f * (bounds.min.y + num - scrollOffset);
		}
		float endValue = contentRect.anchoredPosition.y + num2;
		contentRect.DOAnchorPosY(endValue, moveDuration);
	}
}
