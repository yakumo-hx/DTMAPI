namespace DolocTown.UI;

public class PlatformViewer : CraftViewer<PlatformData>
{
	protected override void OnShow(PlatformData data)
	{
		base.OnShow(data);
		costInfo.text = data.avgCostInfo;
	}

	protected override string GetEmptyInfo()
	{
		return base.staticTexts.PlatformPanelEmpty;
	}
}
