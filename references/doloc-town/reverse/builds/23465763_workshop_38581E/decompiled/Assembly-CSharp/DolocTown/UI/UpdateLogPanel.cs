using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class UpdateLogPanel : DolocUIPanel
{
	[SerializeField]
	private Text textField;

	protected override void __Init()
	{
		base.__Init();
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}

	public void SetText(string text)
	{
		if (!(textField == null))
		{
			textField.text = text;
		}
	}
}
