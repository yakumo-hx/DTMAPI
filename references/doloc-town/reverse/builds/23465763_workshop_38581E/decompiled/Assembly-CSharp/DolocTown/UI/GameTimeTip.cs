using UnityEngine;

namespace DolocTown.UI;

public class GameTimeTip : DolocUiEntity
{
	[SerializeField]
	private RectTransform pauseTip;

	[SerializeField]
	private RectTransform speedUpTip;

	[SerializeField]
	private RectTransform speedUpTipMiddle;

	[SerializeField]
	private RectTransform speedUpOperationTip;

	protected override void __Init()
	{
		base.__Init();
		HideExcept(null);
		SetVisible(value: true);
	}

	public void Hide()
	{
		HideExcept(null);
	}

	private void HideExcept(RectTransform trans)
	{
		pauseTip.gameObject.SetActive(pauseTip == trans);
		speedUpTip.gameObject.SetActive(speedUpTip == trans);
		speedUpTipMiddle.gameObject.SetActive(speedUpTipMiddle == trans);
		speedUpOperationTip.gameObject.SetActive(speedUpOperationTip == trans);
	}

	public void SetPauseTipVisible(bool value)
	{
		if (value)
		{
			HideExcept(pauseTip);
		}
		pauseTip.gameObject.SetActive(value);
	}

	public void SetSpeedUpOperationTipVisible(bool value)
	{
		if (value)
		{
			HideExcept(speedUpOperationTip);
		}
		else
		{
			speedUpOperationTip.gameObject.SetActive(value: false);
		}
	}

	public void ShowSpeedUpTip(bool isMiddle)
	{
		RectTransform trans = (isMiddle ? speedUpTipMiddle : speedUpTip);
		HideExcept(trans);
	}

	public void HideSpeedUpTip()
	{
		speedUpTip.gameObject.SetActive(value: false);
		speedUpTipMiddle.gameObject.SetActive(value: false);
	}
}
