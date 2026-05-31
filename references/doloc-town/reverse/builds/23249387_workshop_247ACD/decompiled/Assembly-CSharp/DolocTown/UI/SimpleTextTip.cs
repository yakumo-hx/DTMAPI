using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class SimpleTextTip : DolocUiRecyclableObject
{
	[SerializeField]
	private Text text;

	public string Description
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

	public void Show()
	{
		base.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}
}
