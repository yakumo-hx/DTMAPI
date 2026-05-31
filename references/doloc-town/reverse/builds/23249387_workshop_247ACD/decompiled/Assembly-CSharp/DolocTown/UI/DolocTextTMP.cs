using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DolocTown.UI;

[RequireComponent(typeof(TMP_Text))]
public class DolocTextTMP : DolocUiRecyclableObject
{
	public TMP_Text textbox { get; private set; }

	public string Text
	{
		get
		{
			return textbox.text;
		}
		set
		{
			textbox.text = value;
		}
	}

	public Color Color
	{
		get
		{
			return textbox.color;
		}
		set
		{
			textbox.color = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		textbox = GetComponent<TMP_Text>();
		textbox.raycastTarget = false;
	}

	public Tween DOFade(float endValue, float duration)
	{
		return textbox.DOFade(endValue, duration);
	}
}
