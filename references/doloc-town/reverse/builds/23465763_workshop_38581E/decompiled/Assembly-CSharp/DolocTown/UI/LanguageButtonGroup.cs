namespace DolocTown.UI;

public class LanguageButtonGroup : VerticalButtonGroup<LanguageButton>
{
	protected override void OnSlotClick(LanguageButton slot)
	{
		base.OnSlotClick(slot);
		slot.SwitchLanguage();
	}
}
