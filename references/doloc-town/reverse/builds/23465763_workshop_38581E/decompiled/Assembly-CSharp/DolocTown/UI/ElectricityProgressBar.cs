using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ElectricityProgressBar : DolocUiObject
{
	[SerializeField]
	private Text text;

	[SerializeField]
	private Image progressbar;

	[SerializeField]
	private Image icon;

	public string Text
	{
		get
		{
			return text.text;
		}
		set
		{
			text.text = value;
		}
	}

	public float Progress
	{
		get
		{
			return progressbar.fillAmount;
		}
		set
		{
			progressbar.fillAmount = value;
		}
	}

	public Color ProgressColor
	{
		get
		{
			return progressbar.color;
		}
		set
		{
			progressbar.color = value;
		}
	}

	public Sprite Icon
	{
		get
		{
			return icon.sprite;
		}
		set
		{
			icon.sprite = value;
		}
	}

	public Color IconColor
	{
		get
		{
			return icon.color;
		}
		set
		{
			icon.color = value;
		}
	}
}
