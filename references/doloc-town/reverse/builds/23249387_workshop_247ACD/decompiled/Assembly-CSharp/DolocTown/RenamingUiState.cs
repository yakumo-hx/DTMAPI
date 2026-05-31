using System;
using DolocTown.UI;

namespace DolocTown;

public class RenamingUiState : DolocUiState<RenamingBox>
{
	private Action<string> onConfirm;

	private Action onExit;

	private string defaultText;

	protected override bool RevertPrevSelected => true;

	protected override bool disablePopDownSound => true;

	public bool HandleStartUpArgs(string title, string holdPlace, string defaultText, Action<string> onConfirm, int maxLength, Action onExit = null)
	{
		this.defaultText = defaultText;
		this.onConfirm = onConfirm;
		this.onExit = onExit;
		base.panel.Render(title, holdPlace, this.defaultText, OnConfirm, maxLength, delegate
		{
			gameController.PopState();
		});
		return true;
	}

	private void OnConfirm(string text)
	{
		onConfirm?.Invoke(text);
		DolocAPI.Delay(0.1f, delegate
		{
			gameController.PopState();
		});
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsConfirmPressed)
		{
			base.panel.TrySelectConfirm(force: false);
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
		onExit?.Invoke();
	}
}
