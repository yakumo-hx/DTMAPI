using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ClickableOperationTipInUI : OperationTipInUI, IPointerClickHandler, IEventSystemHandler
{
	[SerializeField]
	private Image icon;

	private Action onClick;

	public void SetClickCallback(Action onClick)
	{
		this.onClick = onClick;
	}

	public void SetTextKey(string text, Sprite sprite)
	{
		icon.sprite = sprite;
		SetTextKey(text);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		onClick?.Invoke();
	}
}
