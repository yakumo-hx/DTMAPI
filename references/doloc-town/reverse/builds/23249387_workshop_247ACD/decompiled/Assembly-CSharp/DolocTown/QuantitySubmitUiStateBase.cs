using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public abstract class QuantitySubmitUiStateBase<TPanel, TData> : DolocUiState<TPanel> where TPanel : QuantitySubmitPanelBase<TData> where TData : QuantitySubmitData
{
	private TData data;

	private UnityAction<int> onConfirm;

	private UnityAction onCancel;

	private Timer timer = new Timer(DolocAPI.GlobalParameter.QuantitySelectTimer_Ref);

	private bool stopCounting;

	protected override bool RevertPrevSelected => true;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	public bool HandleStartUpArgs(TData data, UnityAction<int> onConfirm, UnityAction onCancel = null)
	{
		if (!data.notEmpty)
		{
			return false;
		}
		this.data = data;
		this.onConfirm = onConfirm;
		this.onCancel = onCancel;
		base.panel.Render(data);
		return true;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseSubOne)
		{
			AddDiff(-1, deltaTime, restart: true);
		}
		else if (userInput.BaseSubOneInProgress)
		{
			AddDiff(-1, deltaTime, restart: false);
		}
		else if (userInput.BaseAddOne)
		{
			AddDiff(1, deltaTime, restart: true);
		}
		else if (userInput.BaseAddOneInProgress)
		{
			AddDiff(1, deltaTime, restart: false);
		}
		else if (userInput.BaseSubTen)
		{
			AddDiff(-10, deltaTime, restart: true);
		}
		else if (userInput.BaseSubTenInProgress)
		{
			AddDiff(-10, deltaTime, restart: false);
		}
		else if (userInput.BaseAddTen)
		{
			AddDiff(10, deltaTime, restart: true);
		}
		else if (userInput.BaseAddTenInProgress)
		{
			AddDiff(10, deltaTime, restart: false);
		}
		else if (userInput.BaseSetMax)
		{
			base.panel.SetMax();
		}
		else if (userInput.BaseSetMin)
		{
			base.panel.SetMin();
		}
		else if (userInput.BaseIsConfirmPressed && base.panel.isRender && !base.panel.inAnimation)
		{
			Confirm(base.panel.currentCount);
		}
		else if (userInput.BaseIsCancelPressed)
		{
			Cancel();
		}
	}

	private void AddDiff(int diff, float deltaTime, bool restart)
	{
		if (restart)
		{
			timer.ReStart();
			stopCounting = !base.panel.AddDiff(diff);
		}
		else if (!stopCounting && timer.Update(deltaTime))
		{
			stopCounting = !base.panel.AddDiff(diff);
		}
	}

	protected override void Register()
	{
		base.panel.onConfirm.AddListener(Confirm);
		base.panel.onCancel.AddListener(Cancel);
	}

	protected override void Unregister()
	{
		base.panel.onConfirm.RemoveListener(Confirm);
		base.panel.onCancel.RemoveListener(Cancel);
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		base.panel.operationTip.SetTextKey(new string[6]
		{
			base.staticTexts.UiTipAddOne,
			base.staticTexts.UiTipSubOne,
			base.staticTexts.UiTipAddTen,
			base.staticTexts.UiTipSubTen,
			base.staticTexts.UiTipSetMax,
			base.staticTexts.UiTipSetMin
		});
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}

	private void Confirm(int value)
	{
		gameController.PopState();
		onConfirm?.Invoke(value);
	}

	private void Cancel()
	{
		gameController.PopState();
		onCancel?.Invoke();
	}
}
