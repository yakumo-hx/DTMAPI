using System;
using DolocTown.UI;

namespace DolocTown;

public class InputBirthdayUiState : DolocUiState<InputBirthdayBox>
{
	private Action<int, int> onConfirm;

	private Timer timer = new Timer(DolocAPI.GlobalParameter.QuantitySelectTimer_Ref);

	private bool stopCounting;

	private InputNumberSlot currentNumberSlot;

	protected override bool RevertPrevSelected => true;

	protected override bool disablePopDownSound => true;

	public bool HandleStartUpArgs(Action<int, int> onConfirm)
	{
		this.onConfirm = onConfirm;
		base.panel.Render(OnConfirm);
		return true;
	}

	private void OnConfirm(int month, int day)
	{
		DolocAPI.DelayFrame(delegate
		{
			onConfirm?.Invoke(month, day);
			gameController.PopState();
		});
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (currentNumberSlot != base.panel.currentNumberSlot)
		{
			currentNumberSlot = base.panel.currentNumberSlot;
			stopCounting = true;
		}
		else if (userInput.BaseIsLeftPressed)
		{
			base.panel.SelectMonth();
		}
		else if (userInput.BaseIsRightPressed)
		{
			base.panel.SelectDay();
		}
		else if (userInput.BaseIsUpPressed)
		{
			AddDiff(1, deltaTime, restart: true);
		}
		else if (userInput.BaseIsUpInProgress)
		{
			AddDiff(1, deltaTime, restart: false);
		}
		else if (userInput.BaseIsDownPressed)
		{
			AddDiff(-1, deltaTime, restart: true);
		}
		else if (userInput.BaseIsDownInProgress)
		{
			AddDiff(-1, deltaTime, restart: false);
		}
		else if (userInput.BaseIsConfirmPressed)
		{
			base.panel.TrySelectConfirm();
		}
		else if (userInput.BaseIsCancelPressed)
		{
			base.panel.TrySelectConfirm();
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
	}

	protected override void Unregister()
	{
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		currentNumberSlot = base.panel.currentNumberSlot;
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}
}
