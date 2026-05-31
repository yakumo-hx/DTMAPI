using DolocTown.Config;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class SystemMenuUiState : DolocUiState<TextMenu>
{
	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		base.panel.Render(new string[4]
		{
			base.staticTexts.UiTipCancel,
			base.staticTexts.UiSystemReturnHomePage,
			DolocConfig.GetL10nText("ui_text_steamstore"),
			base.staticTexts.UiSystemExitGame
		});
		base.panel.MoveToCenter(CenterType.Both);
	}

	protected override void Register()
	{
		base.panel.SetClickCallbacks(OnSystemTextClicked);
	}

	protected override void Unregister()
	{
		base.panel.RemoveCallbacks();
	}

	private void OnSystemTextClicked(int index)
	{
		switch (index)
		{
		case 0:
			gameController.PopState();
			break;
		case 1:
			DolocAPI.ShowQuestionBox(base.staticTexts.UiSystemConfirmHome, delegate
			{
				DolocAPI.ResetMotorStatus();
				DolocAPI.QuitCurrentRoom(null);
				DolocAPI.ReturnHome();
			}, null, firstSelectConfirm: false);
			break;
		case 2:
			Application.OpenURL("https://store.steampowered.com/app/2285550/_/");
			gameController.PopState();
			break;
		case 3:
			DolocAPI.ShowQuestionBox(base.staticTexts.UiSystemConfirmExit, Application.Quit, null, firstSelectConfirm: false);
			break;
		}
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		base.panel.Select(0);
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}
}
