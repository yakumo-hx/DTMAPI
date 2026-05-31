using System.Linq;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown.UI;

public struct CostViewerData : IUIData
{
	public Sprite[] costIcons;

	public int[] costCounts;

	public int[] currentCounts;

	public string[] costCountInfos;

	public bool isEnough;

	public bool isMoneyCostEnough;

	private bool useCostInfo;

	public string costInfo;

	public string[] costTitles;

	public bool notEmpty { get; }

	public string[] itemNames { get; }

	public CostViewerData(CountItem[] costItems, LinearInventory currentInventory, int moneyCost = 0, bool useCostInfo = true)
		: this(costItems, new LinearInventory[1] { currentInventory }, moneyCost, useCostInfo)
	{
	}

	public CostViewerData(CountItem[] costItems, LinearInventory[] inventories, int moneyCost = 0, bool useCostInfo = true)
	{
		this = default(CostViewerData);
		if (inventories.IsNullOrEmpty() || (costItems.IsNullOrEmpty() && moneyCost == 0))
		{
			return;
		}
		notEmpty = true;
		this.useCostInfo = useCostInfo;
		itemNames = costItems.Select((CountItem x) => x.itemName).ToArray();
		costIcons = costItems.Select((CountItem x) => DolocAPI.GetItemSprite(x.itemName)).ToArray();
		costCounts = costItems.Select((CountItem x) => x.itemCount).ToArray();
		currentCounts = costItems.Select((CountItem x) => inventories.CountItem(x.itemName)).ToArray();
		if (moneyCost > 0)
		{
			itemNames = itemNames.Append("money").ToArray();
			costIcons = costIcons.Append(LocSprites.UI_ICON_GOLD28X).ToArray();
			costCounts = costCounts.Append(moneyCost).ToArray();
			currentCounts = currentCounts.Append(DolocAPI.archiveHandle.CurrentMoney).ToArray();
		}
		CheckEnough();
		isMoneyCostEnough = DolocAPI.archiveHandle.CurrentMoney >= moneyCost;
		isEnough &= isMoneyCostEnough;
		if (useCostInfo)
		{
			costTitles = costItems.Select((CountItem x) => DolocAPI.GetItemTitle(x.itemName)).ToArray();
			if (moneyCost > 0)
			{
				costTitles = costTitles.Append(DolocConfig.StaticTexts.ItemMoneyTitle).ToArray();
			}
			costInfo = DolocUtils.GenerateCountItemInfo(costTitles, costCounts, ", ", useWrapSpace: true);
		}
	}

	private CostViewerData(CostViewerData data)
	{
		notEmpty = data.notEmpty;
		isEnough = data.isEnough;
		isMoneyCostEnough = data.isMoneyCostEnough;
		useCostInfo = data.useCostInfo;
		costInfo = data.costInfo;
		itemNames = data.itemNames.ToArray();
		costIcons = data.costIcons.ToArray();
		costCounts = data.costCounts.ToArray();
		currentCounts = data.currentCounts.ToArray();
		costCountInfos = data.costCountInfos.ToArray();
		costTitles = data.costTitles.ToArray();
	}

	private void CheckEnough()
	{
		isEnough = true;
		costCountInfos = new string[costCounts.Length];
		for (int i = 0; i < costCounts.Length; i++)
		{
			bool flag = costCounts[i] <= currentCounts[i];
			isEnough &= flag;
			Color color = (flag ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.SLIENTCOLOR_RED);
			if (currentCounts[i].ToString().Length + costCounts[i].ToString().Length <= 8)
			{
				costCountInfos[i] = $"{currentCounts[i].ToString().Colored(color)}/{costCounts[i]}";
			}
			else
			{
				costCountInfos[i] = $"{currentCounts[i].ToString().Colored(color)}\n/{costCounts[i]}";
			}
		}
	}

	public CostViewerData Multiple(int n)
	{
		CostViewerData result = new CostViewerData(this);
		result.costCounts = result.costCounts.Select((int x) => x * n).ToArray();
		result.CheckEnough();
		if (useCostInfo)
		{
			result.costInfo = DolocUtils.GenerateCountItemInfo(result.costTitles, result.costCounts);
		}
		return result;
	}
}
