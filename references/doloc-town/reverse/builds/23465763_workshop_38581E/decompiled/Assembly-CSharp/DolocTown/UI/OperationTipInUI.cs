using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class OperationTipInUI : DolocUiObject, IInputDeviceDetect
{
	[SerializeField]
	public CanvasGroup canvasGroup;

	[SerializeField]
	private RectTransform rt;

	[SerializeField]
	private TextMeshProUGUI textCmp;

	[SerializeField]
	private SlotLayout layout = SlotLayout.Horizontal;

	private string[] tipText = Array.Empty<string>();

	public void SetTextKey(string text)
	{
		SetTextKey(new string[1] { text });
	}

	public void SetTextKey(string[] tipTexts)
	{
		if (tipTexts.IsNullOrEmpty() || tipTexts.All((string x) => x.IsNullOrEmpty()))
		{
			SetVisible(value: false);
			return;
		}
		SetVisible(value: true);
		tipText = tipTexts;
		OnRefresh(DolocAPI.UserInput.DeviceType);
	}

	public void OnRefresh(DolocInputDeviceType deviceType)
	{
		string[] array = new string[tipText.Length];
		for (int i = 0; i < tipText.Length; i++)
		{
			array[i] = DolocAPI.GetParsedKeystrokeText(deviceType, tipText[i]);
		}
		textCmp.text = string.Join((layout == SlotLayout.Horizontal) ? "\u3000\u3000\u3000" : "\n", array);
		if (base.gameObject.activeSelf)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
		}
	}
}
