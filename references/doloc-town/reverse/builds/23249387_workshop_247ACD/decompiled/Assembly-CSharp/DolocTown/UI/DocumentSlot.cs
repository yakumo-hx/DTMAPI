using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DocumentSlot : DolocNavigationButton
{
	[SerializeField]
	private Text text;

	public string title
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

	public Color txtColor
	{
		get
		{
			return text.color;
		}
		set
		{
			text.color = value;
		}
	}
}
