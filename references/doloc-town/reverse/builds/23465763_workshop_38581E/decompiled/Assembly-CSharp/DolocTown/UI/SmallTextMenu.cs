using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class SmallTextMenu : TextMenu
{
	protected override GameObject slotPrefab => LocPfbs.UI_PFB_TEXT_OPTION;

	protected override void __Init()
	{
		base.__Init();
		base.displayAnimType = UiPanelDisplayAnimType.None;
		slotPool.OnCreate = delegate(TextButton R)
		{
			R.TextAlignment = TextAlignmentOptions.MidlineLeft;
			R.backgroundColor = DolocColor.empty;
		};
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}
}
