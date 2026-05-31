using System;
using DolocTown.Config;
using DolocTown.UI;

namespace DolocTown;

public class LongTextInputUiState : DolocUiState<LongTextInputBox>
{
	private Action<string> onConfirm;

	protected override bool RevertPrevSelected => true;

	protected override bool disablePopDownSound => true;

	public bool HandleStartUpArgs(string title, string holdPlace, Action<string> onConfirm, int maxLength)
	{
		this.onConfirm = onConfirm;
		base.panel.Render(title, holdPlace, OnConfirm, maxLength, delegate
		{
			gameController.PopState();
		});
		return true;
	}

	private void OnConfirm(string text)
	{
		onConfirm?.Invoke(text);
		gameController.PopState();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsConfirmPressed)
		{
			if (base.panel.InputText.IsNullOrEmpty())
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.CalendarPanelInputEmptyHint);
			}
			else
			{
				base.panel.TrySelectConfirm(force: false);
			}
		}
		else if (userInput.BaseIsCancelPressed)
		{
			if (base.panel.isCancelSelected)
			{
				base.panel.cancelButton.FireClick();
			}
			else
			{
				base.panel.cancelButton.Select();
			}
		}
	}

	protected override void Register()
	{
	}

	protected override void Unregister()
	{
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}
}
