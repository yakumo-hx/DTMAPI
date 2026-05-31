using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DolocTown.UI;

public class FactionMissionPanel : DolocUIPanel
{
	[FormerlySerializedAs("missionPanel")]
	[SerializeField]
	public FactionMissionWidget missionWidget;

	[SerializeField]
	public BackpackSideBarWidget backpackPanel;

	protected override void OnStartShow()
	{
		base.OnStartShow();
		SetLeftAndRightLayout(missionWidget, backpackPanel, 48);
		backpackPanel.SetMode(BackpackSideBarMode.Store);
		backpackPanel.RefreshMoney();
	}

	public void RebuildNavigation()
	{
		List<Selectable> list = backpackPanel.allSelectablesArray.ToList();
		if (missionWidget.viewer.allSelectableCount > 0)
		{
			list.AddRange(missionWidget.viewer.allSelectablesArray);
		}
		Selectable[] candidates = list.ToArray();
		backpackPanel.RebuildNavigationHorizontal(candidates, 10f);
		backpackPanel.RebuildNavigationVertical(candidates);
		missionWidget.viewer.RebuildNavigationHorizontal(candidates);
	}
}
