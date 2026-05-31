using System;
using DolocTown.Config;
using DolocTown.Config.UI;
using TMPro;
using UnityEngine;

namespace DolocTown.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DeviceDetectL10nKeyTextCom : DolocUiObject, IInputDeviceDetect
{
	[SerializeField]
	private string actionName;

	[SerializeField]
	private string textKey;

	private GameKeyActionInfo _config;

	private TextMeshProUGUI textCmp;

	public GameKeyActionInfo Config
	{
		get
		{
			if (_config == null)
			{
				_config = DolocConfig.Tables.TbGameKeyAction.GetOrDefault(actionName);
			}
			return _config;
		}
	}

	private TextMeshProUGUI TextCmp
	{
		get
		{
			if (textCmp == null)
			{
				textCmp = GetComponent<TextMeshProUGUI>();
			}
			return textCmp;
		}
	}

	private void Awake()
	{
		Tables.LanguageChange = (Action)Delegate.Combine(Tables.LanguageChange, new Action(RefreshText));
	}

	private void OnDestroy()
	{
		if (DolocConfig.Tables != null)
		{
			Tables.LanguageChange = (Action)Delegate.Remove(Tables.LanguageChange, new Action(RefreshText));
		}
	}

	public void OnRefresh(DolocInputDeviceType deviceType)
	{
		if (DolocAPI.GetActionKeyIconGroup(deviceType, actionName, out var iconGroup))
		{
			string text = "<sprite name=\"" + iconGroup.smallIconUrl + "\">";
			TextCmp.text = text + " " + DolocConfig.GetL10nText(textKey);
		}
	}

	private void RefreshText()
	{
		OnRefresh(DolocAPI.UserInput.DeviceType);
	}
}
