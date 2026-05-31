namespace DolocTown.UI;

public class StoreWidget : DolocPagedLinearUI<StoreItemSlot, StoreItemData>
{
	private MoneyTipSmall moneyTip;

	protected override bool clearOtherDirectionNav => false;

	protected override SlotLayout layout => SlotLayout.Vertical;

	protected override void __Init()
	{
		base.__Init();
		moneyTip = GetComponentInChildren<MoneyTipSmall>();
		moneyTip.Init();
	}

	protected override StoreItemSlot[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<StoreItemSlot>(includeInactive: true);
	}

	protected override void OnInitSlot(StoreItemSlot slot)
	{
	}

	protected override void RenderSlot(StoreItemSlot slot, StoreItemData data)
	{
		slot.Render(data);
	}

	public void SetMoney(int value, bool useAnimation = true)
	{
		moneyTip.SetMoney(value, useAnimation);
	}
}
