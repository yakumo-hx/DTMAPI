using System;
using DolocTown.UI;

namespace DolocTown;

public class QuestionUiState : DolocUiState<QuestionBox>
{
	private Action onConfirm;

	private Action onCancel;

	private bool firstSelectedConfirm;

	protected override bool RevertPrevSelected => true;

	public bool HandleStartUpArgs(string title, Action onConfirm, Action onCancel, bool firstSelectedConfirm)
	{
		base.panel.title = title;
		this.onConfirm = onConfirm;
		this.onCancel = onCancel;
		this.firstSelectedConfirm = firstSelectedConfirm;
		return true;
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		if (firstSelectedConfirm)
		{
			base.panel.buttonConfirm.Select();
		}
		else
		{
			base.panel.buttonCancel.Select();
		}
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
			OnCancel();
		}
	}

	protected override void Register()
	{
		base.panel.buttonConfirm.onClick.AddListener(OnConfirm);
		base.panel.buttonCancel.onClick.AddListener(OnCancel);
	}

	protected override void Unregister()
	{
		base.panel.buttonConfirm.onClick.RemoveListener(OnConfirm);
		base.panel.buttonCancel.onClick.RemoveListener(OnCancel);
	}

	private void OnConfirm()
	{
		DolocAPI.DelayFrame(delegate
		{
			gameController.PopState();
			onConfirm?.Invoke();
		});
	}

	private void OnCancel()
	{
		DolocAPI.DelayFrame(delegate
		{
			gameController.PopState();
			onCancel?.Invoke();
		});
	}
}
