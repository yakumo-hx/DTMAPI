using UnityEngine;
using UnityEngine.Serialization;

namespace DolocTown.UI;

public class AutomateBotPanel : DolocUIPanel
{
	[SerializeField]
	public BackpackSideBarWidget backpackPanel;

	[FormerlySerializedAs("automateBotPanel")]
	[SerializeField]
	public AutomateBotWidget automateBotWidget;

	protected override void __Init()
	{
		base.__Init();
		backpackPanel.maxLineCapacity = 5;
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		SetLeftAndRightLayout(automateBotWidget, backpackPanel, 24);
	}
}
