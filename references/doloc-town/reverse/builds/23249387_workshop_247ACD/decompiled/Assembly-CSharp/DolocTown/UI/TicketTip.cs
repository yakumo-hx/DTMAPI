using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TicketTip : DolocUiObject
{
	private Text text;

	protected override void __Init()
	{
		base.__Init();
		text = GetComponentInChildren<Text>();
	}

	public void SetTicketCount(int count)
	{
		Color color = ((count == 0) ? DolocColor.red : DolocUiColor.TEXTCOLOR_STD);
		text.text = count.ToString();
		text.color = color;
	}
}
