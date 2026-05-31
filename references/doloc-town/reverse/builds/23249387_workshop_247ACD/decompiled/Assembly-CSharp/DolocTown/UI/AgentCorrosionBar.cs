using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AgentCorrosionBar : DolocUiObject
{
	[SerializeField]
	private Image frontImage;

	[SerializeField]
	private Image backImage;

	[SerializeField]
	private Image boardImage;

	[SerializeField]
	private Text text;

	[SerializeField]
	private Color substractColor = Color.red;

	[SerializeField]
	private Color addColor = Color.green;

	[SerializeField]
	private float duration = 0.6f;

	private float __currentValue;

	private Tween currentTween;

	private Sequence seq;

	public float Value
	{
		get
		{
			return __currentValue;
		}
		set
		{
			if (__currentValue != value)
			{
				SetValue(value, value > __currentValue);
			}
		}
	}

	protected override void __Init()
	{
		base.__Init();
		__currentValue = 1f;
	}

	private void SetValue(float value, bool isAdd)
	{
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
			currentTween = frontImage.DOFillAmount(value, 0.5f);
			backImage.fillAmount = value;
			backImage.color = substractColor;
		}
		if ((double)value > 0.7)
		{
			if (seq == null)
			{
				seq = DOTween.Sequence();
				seq.Append(boardImage.DOFade(1f, duration));
				seq.Append(boardImage.DOFade(0f, duration));
				seq.SetLoops(-1);
			}
		}
		else
		{
			seq.Kill();
			seq = null;
			boardImage.DOFade(0f, duration);
		}
	}
}
