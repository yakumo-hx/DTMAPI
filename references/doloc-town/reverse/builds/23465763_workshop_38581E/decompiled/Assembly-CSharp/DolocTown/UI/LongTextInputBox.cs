using System;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown.UI;

public class LongTextInputBox : InputNameBox
{
	[SerializeField]
	public DolocButtonComponent cancelButton;

	[HideInInspector]
	public bool isCancelSelected;

	private Action onCancel;

	public string InputText => inputField.text;

	protected override string SensitivityCharacterWarning => base.staticTexts.InputTextContainsSensitiveWorld;

	protected override void __Init()
	{
		base.__Init();
		cancelButton.onClick.AddListener(delegate
		{
			onCancel();
		});
		cancelButton.onSelect.AddListener(delegate
		{
			isCancelSelected = true;
		});
		cancelButton.onDeselect.AddListener(delegate
		{
			isCancelSelected = false;
		});
	}

	public void Render(string title, string holdPlace, Action<string> onConfirm, int maxLength, Action onCancel)
	{
		Render(title, holdPlace, onConfirm, maxLength);
		isCancelSelected = false;
		this.onCancel = onCancel;
	}

	protected override void OnEdit(string text)
	{
		text = DolocConfig.Tables.CurrentL10nId switch
		{
			"zh-CN" => text.Replace(" ", "\u00a0"), 
			"zh-TW" => text.Replace(" ", "\u00a0"), 
			"ja" => text.Replace(" ", "\u00a0"), 
			"ko" => text.Replace(" ", "\u00a0"), 
			_ => text, 
		};
		base.OnEdit(text);
	}

	protected override void OnConfirm()
	{
		string text = inputField.text.Trim();
		if (text.IsNullOrEmpty())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.CalendarPanelInputEmptyHint);
		}
		else if (ValidateInput(text))
		{
			onConfirm?.Invoke(text);
		}
	}

	protected override string DeleteSpecialCharacter(string text)
	{
		return text;
	}
}
