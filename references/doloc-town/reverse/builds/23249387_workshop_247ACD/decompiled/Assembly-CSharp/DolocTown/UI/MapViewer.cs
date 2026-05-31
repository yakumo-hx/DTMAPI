using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MapViewer : DolocUiObject
{
	[SerializeField]
	private Text txtDesc;

	public void Render(string text)
	{
		if (text.IsNullOrEmpty())
		{
			SetVisible(value: false);
		}
		else
		{
			txtDesc.text = text;
		}
	}
}
