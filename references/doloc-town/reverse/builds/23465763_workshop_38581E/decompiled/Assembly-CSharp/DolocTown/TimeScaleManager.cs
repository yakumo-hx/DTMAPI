using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class TimeScaleManager
{
	private float currentTimeScale;

	private GameTimeTip timeTip => DolocAPI.uiSystem.gameTimeTip;

	public TimeScaleManager()
	{
		currentTimeScale = 1f;
	}

	public void SetTimeScale(float timeScale, bool showTipInMiddle = false)
	{
		float timeScale2 = Mathf.Max(0f, timeScale);
		float num = currentTimeScale;
		currentTimeScale = timeScale2;
		if (!(Mathf.Abs(num - currentTimeScale) < 0.01f))
		{
			Time.timeScale = timeScale2;
			if (timeScale > 1f)
			{
				timeTip.SetSpeedUpOperationTipVisible(value: false);
				timeTip.ShowSpeedUpTip(showTipInMiddle);
			}
			else
			{
				timeTip.HideSpeedUpTip();
			}
		}
	}

	public void RevertTimeScale()
	{
		timeTip.SetSpeedUpOperationTipVisible(value: false);
		SetTimeScale(1f);
	}
}
