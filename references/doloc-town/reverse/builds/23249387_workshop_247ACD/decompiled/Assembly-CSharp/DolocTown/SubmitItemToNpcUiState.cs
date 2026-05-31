using System.Linq;
using DolocTown.Config.Plant;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class SubmitItemToNpcUiState : SubmitSingleItemsUiStateBase
{
	private SubmitItemFilter itemFilter;

	private static int lastSubmitItemCount;

	public static Item lastestItem { get; private set; }

	public bool HandleStartUpArgs(SubmitItemFilter itemFilter)
	{
		if (itemFilter == null)
		{
			return false;
		}
		this.itemFilter = itemFilter;
		lastestItem = null;
		lastSubmitItemCount = 0;
		return true;
	}

	protected override int ItemCountCanSubmit(Item item)
	{
		return itemFilter.targetItemCount;
	}

	protected override bool ItemFilter(Item item)
	{
		return itemFilter.Check(item);
	}

	protected override void OnItemSlotClick(int index)
	{
		Item item = base.panel.itemGetter(index);
		if (itemFilter.CheckSubmittable(item) && DolocAPI.CountItem(item.name) < itemFilter.targetItemCount)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiSubmitNotEnough);
		}
		base.OnItemSlotClick(index);
	}

	protected override void AfterSubmit(Item item, int submitCount, int overflowCount)
	{
		base.AfterSubmit(item, submitCount, overflowCount);
		lastestItem = item;
		lastSubmitItemCount = submitCount;
		for (int i = 0; i < lastSubmitItemCount; i++)
		{
			DolocAPI.BroadcastString(GameEventType.SUBMIT_ITEM, item.name);
		}
	}

	protected override void OnCancel()
	{
		base.OnCancel();
		lastestItem = null;
		lastSubmitItemCount = 0;
	}

	protected override void OnSubmit(int index, int submitCount)
	{
		if (itemFilter.shouldCostItem)
		{
			DolocAPI.CostItemAt(index, submitCount);
		}
		ItemNavSlot slot = base.panel.GetSlot(index);
		DolocAPI.Delay(0.2f, delegate
		{
			DolocAPI.RaiseSpriteFadeUp(DolocAPI.AgentPosition + new Vector3(0f, 2f, 0f), slot.iconSprite);
		});
	}

	protected override void Hide()
	{
		base.Hide();
		itemFilter = null;
	}

	public static bool GetLastSubmitStatus()
	{
		return lastestItem != null;
	}

	public static int GetLastSubmitItemCount()
	{
		return lastSubmitItemCount;
	}

	public static string GetLastSubmitItemGeneTitles()
	{
		if (!(lastestItem is IHasGeneGroup { HasGene: not false } hasGeneGroup))
		{
			return "";
		}
		return hasGeneGroup.GeneGroup.Genes.Select((CropGeneInfo x) => "「" + x.Title + "」").HandleJoinString();
	}

	public static string GetLastSubmitItemSeedType()
	{
		if (!(lastestItem is ItemSeed itemSeed))
		{
			return "";
		}
		return itemSeed.seedProto.SeedType.ToString().ToLower();
	}

	public static int GetLastSubmitItemSeedLifeSpan()
	{
		if (!(lastestItem is ItemSeed itemSeed))
		{
			return 0;
		}
		return itemSeed.seedProto.Lifespan;
	}
}
