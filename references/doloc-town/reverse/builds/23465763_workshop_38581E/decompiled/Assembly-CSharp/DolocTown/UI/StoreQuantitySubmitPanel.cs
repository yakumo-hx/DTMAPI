using System;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class StoreQuantitySubmitPanel : QuantitySubmitPanelBase<StoreQuantitySubmitData>
{
	[SerializeField]
	private Text txtCurrentMoneyInfo;

	[SerializeField]
	private Text txtTotalMoneyInfo;

	[SerializeField]
	private ItemWithQuantity targetItem;

	[SerializeField]
	private HorizontalLayoutGroup infoLayoutGroup;

	private StoreSubmitType _submitType;

	private int unitPrice;

	private string currentMoneyFormatString;

	protected override void __Init()
	{
		base.__Init();
		targetItem.Init();
	}

	public override void Render(StoreQuantitySubmitData data)
	{
		_submitType = data.submitType;
		targetItem.Render(data.itemQuantityData);
		unitPrice = data.unitPrice;
		switch (_submitType)
		{
		case StoreSubmitType.Selling:
			infoLayoutGroup.reverseArrangement = false;
			currentMoneyFormatString = base.staticTexts.StoreQuantitySubmitCurrentMoneySelling;
			break;
		case StoreSubmitType.Buying:
			infoLayoutGroup.reverseArrangement = true;
			currentMoneyFormatString = base.staticTexts.StoreQuantitySubmitCurrentMoneyBuying;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		txtTotalMoneyInfo.text = data.totalMoneyInfo;
		base.Render(data);
	}

	protected override void RefreshView(int count)
	{
		base.RefreshView(count);
		targetItem.count = count;
		txtCurrentMoneyInfo.text = DolocUtils.Format(currentMoneyFormatString, (count * unitPrice).ToString().Colored(DolocUiColor.EYECATCHCOLOR_CYAN));
	}
}
