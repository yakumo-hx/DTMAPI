namespace DolocTown.UI;

public class BuilderItemCostTip : DolocUiEntity
{
	private CostItemViewer costTip;

	protected override void __Init()
	{
		base.__Init();
		costTip = GetComponent<CostItemViewer>();
		costTip.Init();
		costTip.SetDefaultDisplayBoundary();
	}

	public void Render(CostViewerData data)
	{
		costTip.Render(data);
	}

	public void Show()
	{
		SetVisible(value: true);
	}

	public void Hide()
	{
		SetVisible(value: false);
	}
}
