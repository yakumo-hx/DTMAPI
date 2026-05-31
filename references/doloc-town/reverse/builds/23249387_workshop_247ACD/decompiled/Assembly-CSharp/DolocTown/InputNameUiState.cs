using System;
using DolocTown.UI;

namespace DolocTown;

public class InputNameUiState : DolocUiState<InputNameBox>
{
	private Action<string> onConfirm;

	protected override bool RevertPrevSelected => true;

	protected override bool disablePopDownSound => true;

	public bool HandleStartUpArgs(string title, string holdPlace, Action<string> onConfirm, int maxLength)
	{
		this.onConfirm = onConfirm;
		base.panel.Render(title, holdPlace, OnConfirm, maxLength);
		return true;
	}

	private void OnConfirm(string text)
	{
		DolocAPI.DelayFrame(delegate
		{
			onConfirm?.Invoke(text);
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
			base.panel.TrySelectConfirm(force: true);
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
