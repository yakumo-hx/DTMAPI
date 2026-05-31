using DolocTown.Config.Mission;
using UnityEngine;

namespace DolocTown;

public class SubmitItemFilter
{
	private string targetItemName;

	private ItemSubmitConditionInfo submitInfo;

	public int targetItemCount { get; private set; }

	public bool shouldCostItem { get; private set; }

	public SubmitItemFilter(string itemName, int count, bool shouldCostItem)
	{
		targetItemName = itemName;
		targetItemCount = Mathf.Max(1, count);
		this.shouldCostItem = shouldCostItem;
	}

	public SubmitItemFilter(ItemSubmitConditionInfo info)
	{
		submitInfo = info;
		targetItemCount = Mathf.Max(1, info.SubmitCount);
		shouldCostItem = info.ShouldCostItem;
	}

	public bool CheckSubmittable(Item item)
	{
		return CheckCondition(item);
	}

	public bool Check(Item item)
	{
		if (CheckCondition(item))
		{
			return DolocAPI.CountItem(item.name) >= targetItemCount;
		}
		return false;
	}

	private bool CheckCondition(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (item.name == targetItemName)
		{
			return true;
		}
		if (submitInfo == null)
		{
			return false;
		}
		return submitInfo.CheckCondition(item);
	}
}
