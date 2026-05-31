using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BuildingViewer : CraftViewer<BuildingData>
{
	[SerializeField]
	private Text spaceInfo;

	[SerializeField]
	private Text animalSpaceInfo;

	protected override void OnShow(BuildingData data)
	{
		base.OnShow(data);
		spaceInfo.text = data.sizeDescription;
		animalSpaceInfo.text = data.animalSpaceDescription;
	}

	protected override string GetEmptyInfo()
	{
		return base.staticTexts.BuildingPanelEmpty;
	}
}
