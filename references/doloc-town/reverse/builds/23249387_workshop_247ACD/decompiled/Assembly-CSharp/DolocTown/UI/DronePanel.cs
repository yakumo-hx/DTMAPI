using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DronePanel : DolocUIPanel
{
	[SerializeField]
	public BackpackSideBarWidget backpackPanel;

	[SerializeField]
	public DroneWidget droneWidget;

	protected override void OnStartShow()
	{
		base.OnStartShow();
		SetLeftAndRightLayout(droneWidget, backpackPanel);
		backpackPanel.Select(DolocAPI.SelectedItemIndex);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		DolocAPI.HideHoverBox();
		DolocAPI.HideItemBorder();
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		RebuildNavigation();
	}

	public void RebuildNavigation()
	{
		Selectable[] candidates = backpackPanel.allSelectablesArray.Concat(droneWidget.allSelectablesArray).ToArray();
		backpackPanel.RebuildNavigationVertical(backpackPanel.allSelectablesArray, droneWidget.distanceWeight, droneWidget.angleLimit);
		backpackPanel.RebuildNavigationHorizontal(candidates, droneWidget.distanceWeight, droneWidget.angleLimit);
		droneWidget.RebuildNavigationVertical(droneWidget.allSelectablesArray, droneWidget.distanceWeight, droneWidget.angleLimit);
		droneWidget.RebuildNavigationHorizontal(candidates, droneWidget.distanceWeight, droneWidget.angleLimit);
	}
}
