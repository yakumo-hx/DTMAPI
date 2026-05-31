using DolocTown.Config;

namespace DolocTown.UI;

public class StoreQuantitySubmitData : QuantitySubmitData
{
	public string totalMoneyInfo;

	public ItemQuantityData itemQuantityData;

	public int unitPrice;

	public StoreSubmitType submitType;

	public StoreQuantitySubmitData(int maxCount, StoreSubmitType submitType, Item item, int unitPrice, int itemCount, int maxMoney, int initCount)
		: base(maxCount, initCount)
	{
		string text = item.title;
		title = submitType switch
		{
			StoreSubmitType.Selling => DolocUtils.Format(DolocConfig.StaticTexts.StoreQuantitySubmitSelling, text), 
			StoreSubmitType.Buying => DolocUtils.Format(DolocConfig.StaticTexts.StoreQuantitySubmitBuying, text), 
			_ => text, 
		};
		info = DolocConfig.StaticTexts.StoreQuantitySubmitUnitPrice.Format(unitPrice);
		totalMoneyInfo = submitType switch
		{
			StoreSubmitType.Selling => DolocUtils.Format(DolocConfig.StaticTexts.StoreQuantitySubmitTotalMoneySelling, maxMoney.ToString().Colored(DolocUiColor.SLIENTCOLOR_CYAN)), 
			StoreSubmitType.Buying => DolocUtils.Format(DolocConfig.StaticTexts.StoreQuantitySubmitTotalMoneyBuying, maxMoney.ToString().Colored(DolocUiColor.SLIENTCOLOR_CYAN)), 
			_ => text, 
		};
		itemQuantityData = new ItemQuantityData(item, itemCount);
		this.unitPrice = unitPrice;
		this.submitType = submitType;
	}
}
