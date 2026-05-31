using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Recipe;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ItemRecipeSlot : DolocNavigationButton, INavPanel
{
	[SerializeField]
	private CanvasGroup contentGroup;

	[SerializeField]
	private RecipeSubItemSlot outputRecipeItem;

	[SerializeField]
	private InputItemViewer inputItems;

	[SerializeField]
	private GameObject synthesizeGroup;

	[SerializeField]
	private Text synthesizer;

	[SerializeField]
	private Text time;

	[SerializeField]
	private Text hintText;

	public Func<int, RecipeInfo> itemGetter;

	private Action selectCallback;

	public Selectable firstSelected;

	private List<Selectable> selectables = new List<Selectable>();

	private bool ContentVisible
	{
		set
		{
			contentGroup.alpha = (value ? 1 : 0);
			contentGroup.interactable = value;
			contentGroup.blocksRaycasts = value;
		}
	}

	public Selectable[] allSelectablesArray => selectables.ToArray();

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		outputRecipeItem.Init();
		outputRecipeItem.index = -1;
		inputItems.Init();
		inputItems.SetVisible(value: true);
		outputRecipeItem.onPointerEnter.AddListener(delegate
		{
			Item item2 = DolocAPI.GenerateItem(itemGetter?.Invoke(index)?.OutputItem.itemName);
			outputRecipeItem.HoverItemViewer(new ItemData(item2));
		});
		outputRecipeItem.onPointerExit.AddListener(delegate
		{
			outputRecipeItem.HideHoverBox();
		});
		outputRecipeItem.onSelect.AddListener(delegate
		{
			Item item = DolocAPI.GenerateItem(itemGetter?.Invoke(index)?.OutputItem.itemName);
			outputRecipeItem.HoverItemViewer(new ItemData(item));
			selectCallback?.Invoke();
		});
		outputRecipeItem.onDeselect.AddListener(delegate
		{
			outputRecipeItem.HideHoverBox();
		});
		inputItems.itemGetter = delegate(int i)
		{
			CountItem[] array = itemGetter?.Invoke(index)?.InputItems;
			return (array == null || i < 0 || i >= array.Length) ? null : DolocAPI.GenerateItem(array[i].itemName);
		};
	}

	public void SetResetScrollRectCallback(Action<RectTransform> callback)
	{
		outputRecipeItem.resetScrollRectCallback = callback;
		foreach (RecipeSubItemSlot slot in inputItems.slots)
		{
			slot.resetScrollRectCallback = callback;
		}
	}

	public void SetSelectCallback(Action callback)
	{
		selectCallback = callback;
	}

	public void Render(ItemRecipeData data)
	{
		base.interactable = !data.isUnlock;
		backgroundImg.raycastTarget = !data.isUnlock;
		base.BackgroundAlpha = ((!data.isUnlock) ? 1 : 0);
		ContentVisible = data.isUnlock;
		hintText.text = (data.isUnlock ? string.Empty : data.emptyHint);
		if (data.isUnlock)
		{
			outputRecipeItem.Render(data.uiSprite, data.outputCount);
			inputItems.Render(data.simpleData);
			synthesizeGroup.SetActive(data.hasSynthesizer);
			synthesizer.text = data.synthesizerName;
			time.text = data.time;
			RecipeInfo recipeProto = data.recipeProto;
			outputRecipeItem.highLighted = recipeProto.OutputItem.itemName == data.targetItemName;
			for (int i = 0; i < inputItems.slots.Count; i++)
			{
				RecipeSubItemSlot recipeSubItemSlot = inputItems.slots[i];
				recipeSubItemSlot.backgroundImg.raycastTarget = recipeSubItemSlot.index < recipeProto.InputItems.Length;
				recipeSubItemSlot.highLighted = recipeSubItemSlot.index < recipeProto.InputItems.Length && recipeProto.InputItems[i].itemName == data.targetItemName;
				recipeSubItemSlot.onSelect.RemoveListener(OnItemSlotSelect);
				recipeSubItemSlot.onSelect.AddListener(OnItemSlotSelect);
			}
		}
	}

	private void OnItemSlotSelect(int idx)
	{
		selectCallback?.Invoke();
	}

	public void RebuildNavigationHorizontal()
	{
		selectables.Clear();
		if (base.interactable)
		{
			selectables.Add(base.button);
		}
		else
		{
			selectables.Add(outputRecipeItem.button);
			foreach (RecipeSubItemSlot slot in inputItems.slots)
			{
				slot.button.SetNavigation();
				if (slot.interactable)
				{
					selectables.Add(slot.button);
				}
			}
			selectables.RebuildNavigationHorizontal(selectables.ToArray());
		}
		firstSelected = selectables.First();
	}

	protected override void OnGrayed(bool value)
	{
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		this.HideItemBorder();
		selectCallback?.Invoke();
	}
}
