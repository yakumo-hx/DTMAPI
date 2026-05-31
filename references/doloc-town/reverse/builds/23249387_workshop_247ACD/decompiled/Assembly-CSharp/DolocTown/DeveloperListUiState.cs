using DolocTown.UI;
using RedSaw;
using UnityEngine.Events;

namespace DolocTown;

public class DeveloperListUiState : DolocUiState<DeveloperListPanel>
{
	private RSTimer waitTimer = new RSTimer(2f);

	private RSTimer longPressTimer = new RSTimer(1.5f);

	private RSTimer waitForAutoScrollTimer = new RSTimer(3f);

	private bool shouldWait;

	private bool shouldAutoScroll;

	private bool canQuitImmediately;

	public override bool ShowPauseTip => false;

	protected override bool RevertPrevSelected => true;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private IScrollContentRect _contentRect => base.panel;

	protected override bool disablePopUpSound => true;

	protected override bool disablePopDownSound => true;

	protected override void OnUiUpdate(float deltaTime)
	{
		if (shouldWait)
		{
			if (waitTimer.Tick(deltaTime))
			{
				shouldWait = false;
			}
			return;
		}
		if (!canQuitImmediately)
		{
			canQuitImmediately = _contentRect.currentValue < 0.01f;
		}
		if (userInput.BaseIsCancelPressed && canQuitImmediately)
		{
			gameController.PopState();
			return;
		}
		if (userInput.BaseIsCancelPressed)
		{
			longPressTimer.Reset();
		}
		else if (userInput.BaseIsCancelInProgress && longPressTimer.Tick(deltaTime))
		{
			gameController.PopState();
			return;
		}
		if (userInput.GlobalClick)
		{
			shouldAutoScroll = false;
			waitForAutoScrollTimer.Reset();
		}
		if (userInput.BaseScrollDir.magnitude > 0f)
		{
			shouldAutoScroll = false;
			waitForAutoScrollTimer.Reset();
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
		else if (!shouldAutoScroll && waitForAutoScrollTimer.Tick(deltaTime))
		{
			shouldAutoScroll = true;
		}
		if (shouldAutoScroll && !shouldWait && !canQuitImmediately)
		{
			base.panel.AutoScroll(deltaTime);
		}
	}

	protected override void Show()
	{
		base.Show();
		canQuitImmediately = false;
		shouldWait = true;
		waitTimer.Reset();
		shouldAutoScroll = true;
		base.panel.Show();
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	protected override void Register()
	{
	}

	protected override void Unregister()
	{
	}
}
