using DolocTown.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown;

public class IconDescriptor : DolocUiRecyclableObject
{
	[SerializeField]
	private Image imgIcon;

	[SerializeField]
	private Text textDesc;

	public Sprite icon
	{
		get
		{
			return imgIcon.sprite;
		}
		set
		{
			imgIcon.sprite = value;
		}
	}

	public string desc
	{
		get
		{
			return textDesc.text;
		}
		set
		{
			textDesc.text = value;
		}
	}
}
