using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class GameInitTip : DolocUiObject
{
	[SerializeField]
	private Text tipText;

	protected override void __Init()
	{
		base.__Init();
		tipText.text = string.Empty;
	}

	public void Output(string content)
	{
		tipText.text = tipText.text + "\n" + content;
	}

	public void OutputInline(string content)
	{
		if (string.IsNullOrEmpty(tipText.text))
		{
			tipText.text = content;
			return;
		}
		int num = tipText.text.LastIndexOf('\n');
		if (num == -1)
		{
			tipText.text = content;
		}
		else
		{
			tipText.text = tipText.text[..num] + "\n" + content;
		}
	}
}
