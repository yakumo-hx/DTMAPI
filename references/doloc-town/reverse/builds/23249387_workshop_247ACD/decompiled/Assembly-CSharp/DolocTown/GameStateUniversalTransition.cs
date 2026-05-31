using System;

namespace DolocTown;

public class GameStateUniversalTransition : GameStateNoInput
{
	private readonly float fadeInTime;

	private readonly float fadeOutTime;

	private readonly Action callback;

	private readonly float waitTime;

	public GameStateUniversalTransition(GameStateMachine userInput, Action callback = null, float waitTime = 0f, float fadeInTime = 3f, float fadeOutTime = 1.5f)
		: base(userInput)
	{
		this.callback = callback;
		this.waitTime = waitTime;
		this.fadeInTime = fadeInTime;
		this.fadeOutTime = fadeOutTime;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		DolocAPI.ppm.FadeIn(fadeInTime, OnFadeIn);
	}

	private void OnFadeIn()
	{
		callback?.Invoke();
		if (waitTime > 0f)
		{
			DolocAPI.Delay(waitTime, FadeOut);
		}
		else
		{
			FadeOut();
		}
	}

	private void FadeOut()
	{
		DolocAPI.ppm.FadeOut(fadeOutTime, delegate
		{
			gameController.WaitToPopState(this);
		}, shouldReset: false, "FadeOut");
	}
}
