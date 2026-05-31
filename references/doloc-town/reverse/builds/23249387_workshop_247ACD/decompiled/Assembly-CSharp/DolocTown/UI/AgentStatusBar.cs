using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AgentStatusBar : DolocUiObject
{
	[SerializeField]
	private Image frontImage;

	[SerializeField]
	private Image backImage;

	[SerializeField]
	private Image overflowImage;

	[SerializeField]
	private Text text;

	[SerializeField]
	private Color substractColor = Color.red;

	[SerializeField]
	private Color addColor = Color.green;

	private Vector2 parentSize;

	private float __currentValue;

	private float __overflowValue;

	private Tween currentTween;

	public float Value
	{
		get
		{
			return __currentValue;
		}
		set
		{
			SetValue(value, value > __currentValue);
		}
	}

	public float OverflowValue
	{
		get
		{
			return __overflowValue;
		}
		set
		{
			__overflowValue = value;
			if (value == 0f)
			{
				overflowImage.gameObject.SetActive(value: false);
				frontImage.rectTransform.sizeDelta = parentSize;
				return;
			}
			if (!overflowImage.gameObject.activeSelf)
			{
				overflowImage.gameObject.SetActive(value: true);
			}
			float num = __currentValue / (value + __currentValue);
			float num2 = parentSize.x * num;
			frontImage.rectTransform.sizeDelta = new Vector2(num2, parentSize.y);
			overflowImage.rectTransform.sizeDelta = new Vector2(parentSize.x - num2, parentSize.y);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		parentSize = backImage.rectTransform.sizeDelta;
		__currentValue = 1f;
	}

	private void SetValue(float value, bool isAdd)
	{
		SetText(value);
		__currentValue = value;
		currentTween?.Kill();
		if (isAdd)
		{
			currentTween = frontImage.DOFillAmount(value, 0.5f);
			backImage.fillAmount = value;
			backImage.color = addColor;
		}
		else
		{
			currentTween = backImage.DOFillAmount(value, 0.5f);
			frontImage.fillAmount = value;
			backImage.color = substractColor;
		}
	}

	private void SetText(float value)
	{
		if (value >= 1f)
		{
			text.text = "100%";
		}
		else
		{
			text.text = $"{Mathf.RoundToInt(value * 100f)}%";
		}
	}
}
