using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DolocSimpleText : DolocUiRecyclableObject
{
	[SerializeField]
	private Text text;

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

	public Color Color
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
