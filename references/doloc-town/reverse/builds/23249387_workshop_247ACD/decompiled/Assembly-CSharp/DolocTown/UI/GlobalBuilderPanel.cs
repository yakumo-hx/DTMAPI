using UnityEngine;

namespace DolocTown.UI;

public class GlobalBuilderPanel : DolocUIPanel
{
	[SerializeField]
	public ContainerLabelUI containerLabelUI;

	[SerializeField]
	public TempBuildingViewer buildingViewer;

	public override bool redoDisplayAnimation => false;
}
