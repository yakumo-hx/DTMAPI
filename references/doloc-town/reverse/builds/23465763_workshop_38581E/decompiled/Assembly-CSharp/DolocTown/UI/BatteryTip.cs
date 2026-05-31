using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public class BatteryTip : DolocUiRecyclableObject
{
	[SerializeField]
	private Image valueImg;

	[SerializeField]
	private Image statusImg;

	[SerializeField]
	private Text valueText;

	[SerializeField]
	private Gradient gradient;

	public void SetValue(float value)
	{
		if (Mathf.Abs(value - 1f) < 0.01f)
		{
			valueImg.fillAmount = 1f;
			valueImg.color = gradient.Evaluate(1f);
			valueText.text = "100%";
		}
		else
		{
			_SetValue(value);
		}
	}

	private void _SetValue(float value)
	{
		if (valueImg.gameObject.activeSelf)
		{
			value = Mathf.Clamp01(value);
			valueImg.fillAmount = value;
			valueImg.color = gradient.Evaluate(value);
			valueText.text = Mathf.FloorToInt(value * 100f) + "%";
		}
	}

	public void SetStatus(bool isRunning)
	{
		valueImg.gameObject.SetActive(isRunning);
		statusImg.gameObject.SetActive(!isRunning);
		if (isRunning)
		{
			valueText.text = Mathf.FloorToInt(valueImg.fillAmount * 100f) + "%";
		}
		else
		{
			valueText.text = "ERR!";
		}
	}
}
