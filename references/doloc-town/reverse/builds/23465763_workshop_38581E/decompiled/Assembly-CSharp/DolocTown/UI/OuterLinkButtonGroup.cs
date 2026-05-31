namespace DolocTown.UI;

public class OuterLinkButtonGroup : VerticalButtonGroup<LinkButton>
{
	protected override void OnSlotClick(LinkButton slot)
	{
		base.OnSlotClick(slot);
		slot.OpenOuterLink();
	}
}
