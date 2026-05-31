using TMPro;
using UnityEngine;

namespace DolocTown.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DeviceDetectTextCom : DolocUiObject, IInputDeviceDetect
{
	public string actionName;

	private string text;

	private TextMeshProUGUI textCmp;

	public string Text
	{
		get
		{
			return text;
		}
		set
		{
			text = value;
			OnRefresh(DolocAPI.UserInput.DeviceType);
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

	public void OnRefresh(DolocInputDeviceType deviceType)
	{
		if (DolocAPI.GetActionKeyIconGroup(deviceType, actionName, out var iconGroup))
		{
			string text = "<sprite name=\"" + iconGroup.smallIconUrl + "\">";
			TextCmp.text = text + " " + this.text;
		}
	}

	private void _Refresh()
	{
		OnRefresh(DolocInputDeviceType.XboxController);
	}
}
