using System.Collections.Generic;
using System.Linq;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class HomePageUiState : DolocUiState<HomePage>
{
	private int currentIndex;

	private bool shouldWork;

	private List<ButtonAction> buttonActions = new List<ButtonAction>();

	private HomePageTextMenu textMenu => base.panel.textMenu;

	protected override bool disablePopUpSound => true;

	protected override bool disablePopDownSound => true;

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		buttonActions.Clear();
		int num = 0;
		buttonActions.Add(new ButtonAction(num++, () => base.staticTexts.UiTipHomepageStartGame, delegate
		{
			DolocAPI.EnterUI<GameDataUiState>();
		}));
		buttonActions.Add(new ButtonAction(num++, () => base.staticTexts.UiTipHomepageSettings, delegate
		{
			DolocAPI.EnterUI((SettingPanelUiState state) => state.HandleStartUpArgs(useRaycastMask: true));
		}));
		buttonActions.Add(new ButtonAction(num++, () => base.staticTexts.UiTipHomepageMods, delegate
		{
			DolocAPI.EnterUI<ModUiState>();
		}));
		buttonActions.Add(new ButtonAction(num++, () => base.staticTexts.UiTipHomepageDeveloperList, delegate
		{
			DolocAPI.EnterUI(delegate(DeveloperListUiState state)
			{
				base.panel.Hide();
				state.DisposableExit = delegate
				{
					base.panel.Show();
				};
				return true;
			});
		}));
		buttonActions.Add(new ButtonAction(num++, () => base.staticTexts.UiTipHomepageExitGame, Application.Quit));
	}

	private void RenderTextMenu()
	{
		textMenu.Render(buttonActions.Select((ButtonAction x) => x.text).ToArray());
		base.panel.BuildNavigation();
	}

	private void OnClick(int index)
	{
		if (shouldWork)
		{
			currentIndex = index;
			if (index >= 0 && index < buttonActions.Count)
			{
				buttonActions[index].action?.Invoke();
			}
		}
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (shouldWork)
		{
			if (userInput.BaseIsCancelPressed)
			{
				textMenu.SelectLast();
			}
			else if (userInput.BaseIsMove && !base.panel.isFocused)
			{
				textMenu.GetFocus();
			}
		}
	}

	protected override void Show()
	{
		base.Show();
		shouldWork = false;
		base.panel.Show();
		RenderTextMenu();
		textMenu.Show(useTween: true, delegate
		{
			shouldWork = true;
			textMenu.SelectFirst();
		});
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	protected override void Register()
	{
		textMenu.SetClickCallbacks(OnClick);
	}

	protected override void Unregister()
	{
		textMenu.RemoveCallbacks();
	}

	public override void OnPause()
	{
		base.OnPause();
		textMenu.LoseFocus();
	}

	public override void OnResume()
	{
		base.OnResume();
		Refresh();
	}

	public void Refresh()
	{
		RenderTextMenu();
		textMenu.GetFocus();
		base.panel.RefreshText();
	}
}
