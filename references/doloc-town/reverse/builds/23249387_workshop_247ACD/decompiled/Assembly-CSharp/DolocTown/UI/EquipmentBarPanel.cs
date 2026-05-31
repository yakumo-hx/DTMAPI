using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EquipmentBarPanel : DolocUIPanel
{
	[SerializeField]
	public BackpackBottomPanel backpackPanel;

	[SerializeField]
	public EquipmentBarWidget equipmentBar;

	[SerializeField]
	public float distanceWeight = 10f;

	[SerializeField]
	public float angleLimit = 90f;

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		RebuildNavigation();
	}

	public void RebuildNavigation()
	{
		Selectable[] candidates = backpackPanel.allSelectablesArray.Concat(equipmentBar.allSelectablesArray).ToArray();
		equipmentBar.RebuildNavigationVertical(candidates, distanceWeight, angleLimit, wrapAround: false);
		equipmentBar.RebuildNavigationHorizontal(equipmentBar.allSelectablesArray, distanceWeight, angleLimit, wrapAround: false);
		backpackPanel.RebuildNavigationVertical(candidates, distanceWeight, angleLimit, wrapAround: false);
	}
}
