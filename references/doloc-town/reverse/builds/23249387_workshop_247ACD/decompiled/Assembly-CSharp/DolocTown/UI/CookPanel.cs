using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CookPanel : DolocUIPanel
{
	[SerializeField]
	public RecipePanel recipePanel;

	[SerializeField]
	public BackpackSideBarWidget backpackPanel;

	[SerializeField]
	public DishPanel dishPanel;

	private CookPanelMode currentMode;

	private UnityEvent onClose = new UnityEvent();

	public override bool activeAllWidgetOnShow => false;

	public override UnityEvent OnCloseButtonClick => onClose;

	protected override void __Init()
	{
		base.__Init();
		recipePanel.OnCloseButtonClick.AddListener(onClose.Invoke);
		dishPanel.OnCloseButtonClick.AddListener(onClose.Invoke);
	}

	public void InitSwitchBar(IRecipeGroup group1, IRecipeGroup group2)
	{
		recipePanel.switchBar.SetTitle(group1?.SwitchMainInfo, group2?.SwitchSubInfo);
		dishPanel.switchBar.SetTitle(group2?.SwitchMainInfo, group1?.SwitchSubInfo);
	}

	public void SetMode(CookPanelMode mode)
	{
		if (!base.isRender)
		{
			currentMode = mode;
			return;
		}
		_ = currentMode;
		currentMode = mode;
		bool useTween = base.isRender;
		switch (mode)
		{
		case CookPanelMode.OnlyFixed:
		case CookPanelMode.Fixed:
			DolocAPI.HideItemBorder();
			DolocAPI.HideHoverBox();
			recipePanel.Show(useTween);
			recipePanel.GetFocus();
			dishPanel.Hide(useTween);
			backpackPanel.Hide(useTween);
			break;
		case CookPanelMode.OnlyDynamic:
		case CookPanelMode.Dynamic:
			recipePanel.Hide(useTween);
			SetLeftAndRightLayout(dishPanel, backpackPanel, 28);
			backpackPanel.SetCloseButtonVisible(value: false);
			dishPanel.Show(useTween, RefreshDishPanelNavigation);
			backpackPanel.Show(useTween);
			DolocAPI.DelayFrame(backpackPanel.GetFocus);
			break;
		default:
			throw new ArgumentOutOfRangeException("mode", mode, null);
		}
	}

	public void RefreshDishPanelNavigation()
	{
		Selectable[] candidates = backpackPanel.allSelectablesArray.Concat(dishPanel.allSelectablesArray).ToArray();
		backpackPanel.RebuildNavigationHorizontal(candidates);
		backpackPanel.RebuildNavigationVertical(backpackPanel.allSelectablesArray);
		dishPanel.RebuildNavigationHorizontal(candidates);
		dishPanel.RebuildNavigationVertical(candidates);
	}

	public void SetTaskInfo(string text)
	{
		recipePanel.SetTaskInfo(text);
		dishPanel.SetTaskInfo(text);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		SetMode(currentMode);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		DolocAPI.HideItemBorder();
		DolocAPI.HideHoverBox();
		recipePanel.Hide();
		dishPanel.Hide();
		backpackPanel.Hide();
	}

	public void GetFocus()
	{
		switch (currentMode)
		{
		case CookPanelMode.OnlyFixed:
		case CookPanelMode.Fixed:
			recipePanel.GetFocus();
			break;
		case CookPanelMode.OnlyDynamic:
		case CookPanelMode.Dynamic:
			backpackPanel.GetFocus();
			break;
		}
	}
}
