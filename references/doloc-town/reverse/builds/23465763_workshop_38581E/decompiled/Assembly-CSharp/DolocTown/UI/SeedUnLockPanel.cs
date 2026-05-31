namespace DolocTown.UI;

public class SeedUnLockPanel : DolocPagedGridUI<SeedUINode, SeedUnLockData>
{
	protected override SeedUINode[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<SeedUINode>(includeInactive: true);
	}

	protected override void OnInitSlot(SeedUINode slot)
	{
		slot.onSelect.AddListener(delegate
		{
			slot.Selected = true;
		});
		slot.onDeselect.AddListener(delegate
		{
			slot.Selected = false;
		});
	}

	protected override void RenderSlot(SeedUINode slot, SeedUnLockData data)
	{
		slot.Render(data);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		DolocAPI.HideHoverBox();
	}
}
