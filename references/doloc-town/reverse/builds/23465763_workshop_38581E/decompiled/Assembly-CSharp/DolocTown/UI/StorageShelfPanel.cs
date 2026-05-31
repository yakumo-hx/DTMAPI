using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DolocTown.UI;

public class StorageShelfPanel : AutoSizeUIPanel
{
	[SerializeField]
	public BackpackBottomPanel backpackPanel;

	[FormerlySerializedAs("shelfPanel")]
	[SerializeField]
	public StorageShelfWidget shelfWidget;

	private VerticalLayoutGroup verticalLayoutGroup;

	private float distanceWeight => shelfWidget.distanceWeight;

	private float angleLimit => shelfWidget.angleLimit;

	protected override void __Init()
	{
		base.__Init();
		verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
	}

	public void RebuildNavigation()
	{
		Selectable[] candidates = backpackPanel.allSelectablesArray.Concat(shelfWidget.allSelectablesArray).ToArray();
		backpackPanel.RebuildNavigationVertical(candidates, distanceWeight, angleLimit);
		backpackPanel.RebuildNavigationHorizontal(backpackPanel.allSelectablesArray, distanceWeight, angleLimit);
		shelfWidget.RebuildNavigationVertical(candidates, distanceWeight, angleLimit);
		shelfWidget.RebuildNavigationHorizontal(shelfWidget.allSelectablesArray, distanceWeight, angleLimit);
	}

	public void RefreshLayout()
	{
		VerticalLayoutGroup verticalLayoutGroup = this.verticalLayoutGroup;
		verticalLayoutGroup.spacing = backpackPanel.rowCount switch
		{
			1 => 240, 
			2 => 120, 
			_ => 20, 
		};
		RebuildLayout();
	}
}
