using System;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(HorizontalLayoutGroup))]
public class OperationTipBase : DolocUiRecyclableObject
{
	[SerializeField]
	private CanvasGroup _canvasGroup;

	[SerializeField]
	private Text textPrompt;

	[SerializeField]
	private Image imageKey;

	protected float Alpha
	{
		get
		{
			return _canvasGroup.alpha;
		}
		set
		{
			_canvasGroup.alpha = value;
		}
	}

	public string Prompt
	{
		get
		{
			return textPrompt.text;
		}
		set
		{
			Render(imageKey.sprite, value);
		}
	}

	protected void Render(Sprite sprite, string prompt, Action callback = null)
	{
		SetSprite(imageKey, sprite, autoSize: true);
		textPrompt.text = prompt;
		textPrompt.fontStyle = (DolocAPI.userSettings.operationTextBlod ? FontStyle.Bold : FontStyle.Normal);
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.rectTransform);
		callback?.Invoke();
	}
}
