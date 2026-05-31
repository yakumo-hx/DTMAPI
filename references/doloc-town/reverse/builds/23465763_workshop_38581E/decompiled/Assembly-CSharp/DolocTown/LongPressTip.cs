using System;
using DolocTown.Config;
using DolocTown.UI;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class LongPressTip
{
	private readonly LongPressTimer timer;

	private readonly Action callback;

	private LongPressTipRenderer skipTip;

	private bool shouldUpdate = true;

	public LongPressTip(LongPressTipRenderer tip, float length = 3f, Action callback = null, string infoKey = "long_press_tip_jump")
	{
		timer = new LongPressTimer(length);
		skipTip = tip;
		skipTip.Info = DolocConfig.GetL10nText(infoKey);
		skipTip.Progress = 0f;
		this.callback = callback;
	}

	public void Update(bool cancelC, float dt)
	{
		if (!shouldUpdate)
		{
			return;
		}
		if (cancelC)
		{
			skipTip.Progress = timer.Progress;
			if (timer.TickHold(dt))
			{
				shouldUpdate = false;
				callback?.Invoke();
			}
		}
		else if (timer.HasProgress)
		{
			timer.TickRelease(dt);
			skipTip.Progress = timer.Progress;
		}
	}

	public void Dispose()
	{
		if (skipTip != null)
		{
			UnityEngine.Object.Destroy(skipTip.gameObject);
		}
	}
}
