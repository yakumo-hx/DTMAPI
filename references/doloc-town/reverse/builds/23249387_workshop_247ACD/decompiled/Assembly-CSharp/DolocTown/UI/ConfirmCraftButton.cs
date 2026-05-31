using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ConfirmCraftButton : DolocNavigationButton
{
	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Color grayColor;

	[SerializeField]
	private DeviceDetectTextCom textCom;

	[SerializeField]
	private Text text;

	[SerializeField]
	public Image progress;

	public string buttonText
	{
		set
		{
			textCom.Text = value;
			if (text != null)
			{
				text.text = value;
			}
		}
	}

	protected override void OnGrayed(bool value)
	{
		SetButtonColor(value ? grayColor : normalColor);
	}

	private void SetButtonColor(Color color)
	{
		ColorBlock colors = base.button.colors;
		colors.normalColor = color;
		colors.highlightedColor = color;
		base.button.colors = colors;
	}
}
