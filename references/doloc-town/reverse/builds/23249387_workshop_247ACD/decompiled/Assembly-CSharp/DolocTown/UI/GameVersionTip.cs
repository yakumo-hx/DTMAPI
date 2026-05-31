using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Text))]
public class GameVersionTip : DolocBasicTip
{
	protected override void __Init()
	{
		base.__Init();
		bool visible = !DolocAPI.gameManager.gameInitConfig.hideVersionInfo;
		SetVisible(visible);
	}

	public override void SetVisible(bool value)
	{
		base.SetVisible(value);
		GetComponent<Text>().text = string.Format(base.staticTexts.UiTipGameVersion, Application.version);
	}
}
