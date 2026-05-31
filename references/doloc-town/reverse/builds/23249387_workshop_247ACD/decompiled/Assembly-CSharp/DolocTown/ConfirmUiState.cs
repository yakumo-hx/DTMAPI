using System;
using DolocTown.Config.UI;
using DolocTown.UI;

namespace DolocTown;

public class ConfirmUiState : DolocUiState<ConfirmBox>
{
	private Action onConfirm;

	protected override bool RevertPrevSelected => true;

	public bool HandleStartUpArgs(string content, Action onConfirm)
	{
		base.panel.SetContent(content);
		this.onConfirm = onConfirm;
		return true;
	}

	public bool HandleStartUpArgs(AlignmentText title, AlignmentText content, AlignmentText signature, Action onConfirm)
	{
		base.panel.SetContent(title, content, signature);
		this.onConfirm = onConfirm;
		return true;
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		base.panel.buttonConfirm.Select();
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			OnConfirm();
		}
	}

	protected override void Register()
	{
		base.panel.buttonConfirm.onClick.AddListener(OnConfirm);
	}

	protected override void Unregister()
	{
		base.panel.buttonConfirm.onClick.RemoveListener(OnConfirm);
	}

	private void OnConfirm()
	{
		DolocAPI.DelayFrame(delegate
		{
			gameController.PopState();
			onConfirm?.Invoke();
		});
	}
}
