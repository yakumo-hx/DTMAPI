using System;
using DolocTown.Config;

namespace DolocTown.UI;

public class BotRecipeViewer : DolocPagedGridUI<BotRecipeSlot, BotRecipeData>
{
	public Action RefreshViewCallBack;

	protected override BotRecipeSlot[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<BotRecipeSlot>(includeInactive: true);
	}

	protected override void OnInitSlot(BotRecipeSlot slot)
	{
		slot.onSelect.AddListener(delegate
		{
			slot.backgroundColor = slot.selectedColor;
		});
		slot.onDeselect.AddListener(delegate
		{
			slot.backgroundColor = (slot.Using ? slot.usedColor : slot.normalColor);
		});
	}

	protected override void RenderSlot(BotRecipeSlot slot, BotRecipeData data)
	{
		slot.Render(data);
		slot.backgroundColor = (data.used ? slot.usedColor : slot.normalColor);
		slot.backgroundColor = (slot.IsSelected ? slot.selectedColor : slot.backgroundColor);
	}

	protected override void OnStartShow()
	{
		SetEmptyInfo(DolocConfig.StaticTexts.AutomateBotPanelRecipeEmpty);
	}

	protected override void OnRefreshView()
	{
		base.OnRefreshView();
		RefreshViewCallBack?.Invoke();
	}
}
