using System;
using System.Text.RegularExpressions;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class InputNameBox : DolocUIPanel
{
	[SerializeField]
	protected TextMeshProUGUI titleText;

	[SerializeField]
	protected DolocInputFiledComponent inputField;

	[SerializeField]
	protected Image headIcon;

	[SerializeField]
	protected TextMeshProUGUI hintText;

	[SerializeField]
	private DolocButtonComponent confirmButton;

	protected Action<string> onConfirm;

	public bool isOpenVirtualKeyboard;

	private SensitiveWordChecker sensitiveWordChecker;

	protected int maxLength;

	private string holdPlaceText;

	private bool editEnd;

	private string latestGamepadInputText;

	private bool isSteamSDKInitialized => DolocAPI.gameManager.IsSteamSDKInitialized;

	protected virtual string SensitivityCharacterWarning => base.staticTexts.InputNameContainsSensitiveWorld;

	protected override void __Init()
	{
		base.__Init();
		inputField.onSelect.AddListener(StartEdit);
		inputField.onValueChanged.AddListener(OnEdit);
		inputField.onEndEdit.AddListener(EndEdit);
		confirmButton.onClick.AddListener(OnConfirm);
		sensitiveWordChecker = new SensitiveWordChecker();
	}

	private void StartEdit()
	{
		editEnd = false;
		CheckIfNeedOpenVirtualKeyboard();
	}

	private void EndEdit(string _)
	{
		DolocAPI.DelayFrame(delegate
		{
			editEnd = true;
		});
		Debug.Log("结束输入");
		confirmButton.Select();
	}

	private void CheckIfNeedOpenVirtualKeyboard()
	{
		try
		{
			if (DolocAPI.UserInput.DeviceType == DolocInputDeviceType.KeyboardMouse)
			{
				return;
			}
			if (isSteamSDKInitialized)
			{
				isOpenVirtualKeyboard = SteamUtils.ShowGamepadTextInput(EGamepadTextInputMode.k_EGamepadTextInputModeNormal, EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine, titleText.text, (uint)maxLength, inputField.text);
			}
			else
			{
				Debug.LogError("SteamManager is not initialized");
			}
		}
		catch (Exception message)
		{
			isOpenVirtualKeyboard = false;
			Debug.LogError(message);
		}
		if (!isOpenVirtualKeyboard)
		{
			Debug.LogError("Open virtual keyboard failed");
		}
	}

	private void Update()
	{
		if (!isOpenVirtualKeyboard)
		{
			return;
		}
		uint enteredGamepadTextLength = SteamUtils.GetEnteredGamepadTextLength();
		if (enteredGamepadTextLength == 0)
		{
			return;
		}
		SteamUtils.GetEnteredGamepadTextInput(out var pchText, enteredGamepadTextLength);
		if (!(pchText == latestGamepadInputText))
		{
			latestGamepadInputText = pchText;
			string text = TrimText(pchText);
			text = DeleteSpecialCharacter(text);
			if (!text.IsNullOrEmpty())
			{
				inputField.text = pchText;
			}
		}
	}

	public void Render(string title, string holdPlace, Action<string> onConfirm, int maxLength)
	{
		titleText.text = title;
		this.onConfirm = onConfirm;
		this.maxLength = maxLength;
		holdPlaceText = holdPlace;
		inputField.text = holdPlace;
		SetSprite(headIcon, DolocAPI.GlobalParameter.UiPlayerDefaultPortrait.Asset);
	}

	public void TrySelectConfirm(bool force)
	{
		if (force || editEnd)
		{
			confirmButton.Select();
		}
	}

	protected bool CheckText()
	{
		string text = inputField.text;
		if (inputField.text.IsNullOrEmpty())
		{
			inputField.text = holdPlaceText;
			return false;
		}
		return ValidateInput(text);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		editEnd = false;
		SetHint();
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		EventSystem.current?.SetSelectedGameObject(null);
		inputField.Select();
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		onConfirm = null;
		isOpenVirtualKeyboard = false;
		latestGamepadInputText = null;
		if (isSteamSDKInitialized)
		{
			SteamUtils.DismissGamepadTextInput();
		}
	}

	protected void SetHint(string text = "")
	{
		hintText.text = text ?? string.Empty;
	}

	protected virtual void OnEdit(string text)
	{
		ValidateInput(text.TrimStart());
	}

	protected bool ValidateInput(string text)
	{
		if (text.IsNullOrEmpty())
		{
			SetHint();
			return false;
		}
		text = TrimText(text);
		text = DeleteSpecialCharacter(text);
		inputField.text = text;
		if (!ValidateTextNotSensitive(text))
		{
			return false;
		}
		SetHint();
		return true;
	}

	protected virtual void OnConfirm()
	{
		string text = inputField.text;
		if (CheckText())
		{
			DolocAPI.ShowQuestionBox(DolocUtils.Format(base.staticTexts.InputNameConfirm, text.Colored(DolocUiColor.SLIENTCOLOR_BLUE)), delegate
			{
				onConfirm?.Invoke(text);
			});
		}
	}

	protected virtual string DeleteSpecialCharacter(string text)
	{
		if (DolocAPI.CurrentL10nId == "zh-CN" || DolocAPI.CurrentL10nId == "zh-TW")
		{
			return Regex.Replace(text, "[^\\u4e00-\\u9fa5a-zA-Z0-9]", "");
		}
		return Regex.Replace(text, "\\s+", "");
	}

	private string TrimText(string text)
	{
		if (text == null)
		{
			text = "";
		}
		int num = 0;
		string text2 = "";
		string text3 = text;
		for (int i = 0; i < text3.Length; i++)
		{
			char c = text3[i];
			int num2 = ((!IsChineseCharacter(c)) ? 1 : 2);
			if (num + num2 > maxLength)
			{
				break;
			}
			text2 += c;
			num += num2;
		}
		return text2;
	}

	private static bool IsChineseCharacter(char c)
	{
		return Regex.IsMatch(c.ToString(), "[\\u4e00-\\u9fa5]");
	}

	private bool ValidateTextNotSensitive(string text)
	{
		if (sensitiveWordChecker.ContainsSensitiveWord(text))
		{
			SetHint(SensitivityCharacterWarning);
			return false;
		}
		return true;
	}
}
