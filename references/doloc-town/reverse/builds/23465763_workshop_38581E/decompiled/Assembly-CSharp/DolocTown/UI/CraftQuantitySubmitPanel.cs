using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CraftQuantitySubmitPanel : QuantitySubmitPanelBase<CraftQuantityData>
{
	[SerializeField]
	private CostItemViewer costViewer;

	[SerializeField]
	private CostItemSlot targetItem;

	[SerializeField]
	private Text txtTime;

	protected override void __Init()
	{
		base.__Init();
		costViewer.Init();
		targetItem.Init();
	}

	public override void Render(CraftQuantityData data)
	{
		base.Render(data);
		costViewer.Render(data.recipeData.itemCosts.Multiple(data.initCount));
		targetItem.Render(data.recipeData.outputItem, data.recipeData.outputItemSprite, data.recipeData.GetTargetCount(data.initCount));
	}

	protected override void RefreshView(int count)
	{
		base.RefreshView(count);
		costViewer.Render(currentData.recipeData.itemCosts.Multiple(count));
		targetItem.Render(currentData.recipeData.outputItem, currentData.recipeData.outputItemSprite, currentData.recipeData.GetTargetCount(count));
		SetText(txtTime, currentData.recipeData.GetFormatTimeString(count));
	}
}
