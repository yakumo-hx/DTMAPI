using System.Collections.Generic;
using DolocTown.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AutomateBotViewer : DolocUIPanel
{
	[SerializeField]
	private Text botName;

	[SerializeField]
	private Text runningState;

	[SerializeField]
	public OptionItem OptionItemUI1;

	[SerializeField]
	public OptionItem OptionItemUI2;

	[SerializeField]
	public BotRecipeViewer RecipeViewer;

	[SerializeField]
	private CanvasGroup contentViewer;

	[SerializeField]
	private CanvasGroup contentCfg;

	[SerializeField]
	private Text emptyCfg;

	[SerializeField]
	private Text emptyContent;

	protected override void __Init()
	{
		base.__Init();
		OptionItemUI1.Init();
		OptionItemUI2.Init();
		RecipeViewer.Init();
		RecipeViewer.WrapAround = false;
	}

	public void RenderView(AutomateBotData data)
	{
		emptyCfg.text = DolocConfig.StaticTexts.AutomateBotPanelCfgEmpty;
		emptyContent.text = DolocConfig.StaticTexts.AutomateBotPanelDateEmpty;
		contentViewer.alpha = (data.notEmpty ? 1 : 0);
		emptyContent.gameObject.SetActive(!data.notEmpty);
		if (data.notEmpty)
		{
			botName.text = data.botName;
			runningState.text = data.runningState;
			emptyCfg.gameObject.SetActive(!data.hasCfg);
			contentCfg.alpha = (data.hasCfg ? 1 : 0);
			if (data.hasCfg)
			{
				OptionItemUI1.SetVisible(data.optionItem1);
				OptionItemUI2.SetVisible(data.optionItem2);
				RecipeViewer.SetVisible(data.hasRecipeList);
				OptionItemUI1.Title = data.optionItem1Title;
				OptionItemUI2.Title = data.optionItem2Title;
				OptionItemUI1.SetBgImageAlpha(0);
				OptionItemUI2.SetBgImageAlpha(0);
				RebuildNavigation(data);
			}
		}
	}

	private void RebuildNavigation(AutomateBotData data)
	{
		List<Selectable> selectables = new List<Selectable>();
		if (data.optionItem1)
		{
			selectables.Add(OptionItemUI1.Selectable);
		}
		if (data.optionItem2)
		{
			selectables.Add(OptionItemUI2.Selectable);
		}
		selectables.ToArray().RebuildNavigationVerticalByOrder(wrapAround: false);
		if (selectables.Count > 0)
		{
			EventSystem.current?.SetSelectedGameObject(null);
			selectables[0].Select();
		}
		RecipeViewer.RefreshViewCallBack = delegate
		{
			DolocAPI.Delay(0.05f, delegate
			{
				if (data.hasRecipeList && RecipeViewer.TotalCapacity != 0)
				{
					selectables[^1].SetNavigationOnDown(RecipeViewer.slots[0].button);
					if (RecipeViewer.currentPageIndex == 0)
					{
						RecipeViewer.slots[0].button.SetNavigationOnUp(selectables[^1]);
						if (RecipeViewer.RealSlotCountPerPage >= 2)
						{
							RecipeViewer.slots[1].button.SetNavigationOnUp(selectables[^1]);
						}
					}
					if (RecipeViewer.currentPageIndex == RecipeViewer.TotalPageCount - 1)
					{
						RecipeViewer.slots[RecipeViewer.RealSlotCountPerPage - 1].button.SetNavigationOnDown(selectables[0]);
						if (RecipeViewer.RealSlotCountPerPage % 2 == 0)
						{
							RecipeViewer.slots[RecipeViewer.RealSlotCountPerPage - 2].button.SetNavigationOnDown(selectables[0]);
						}
					}
				}
			});
		};
	}
}
