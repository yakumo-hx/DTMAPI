using DolocTown.Config;
using DolocTown.Config.EnvOptimizer;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class EnvOptimizerComponentSlot
{
	private Item currentItem;

	[JsonProperty]
	private bool isActive;

	[JsonProperty]
	private DateInfo activeDateInfo;

	[JsonProperty]
	public string itemName => currentItem?.name ?? string.Empty;

	public bool IsActive => isActive;

	public bool IsEmpty => currentItem == null;

	public Item CurrentItem => currentItem;

	public DateInfo ActiveDateInfo => activeDateInfo;

	public EnvOptimizerSlotInfo SlotInfo => DolocConfig.Tables.TbEnvOptimizerSlot.GetById(itemName ?? "");

	public EnvOptimizerComponentSlot()
	{
		currentItem = null;
		isActive = false;
	}

	[JsonConstructor]
	private EnvOptimizerComponentSlot(string itemName, bool isActive, DateInfo activeDateInfo)
	{
		currentItem = (itemName.IsNullOrEmpty() ? null : DolocAPI.GenerateItem(itemName));
		this.isActive = currentItem != null && isActive;
		this.activeDateInfo = activeDateInfo;
	}

	public Item TakeItem()
	{
		if (IsEmpty || !IsActive)
		{
			return null;
		}
		Item result = currentItem;
		currentItem = null;
		return result;
	}

	public bool PlaceItem(Item item)
	{
		if (isActive)
		{
			return false;
		}
		currentItem = item;
		return true;
	}

	public bool SetActive(DateInfo dateInfo)
	{
		isActive = !IsEmpty;
		if (isActive)
		{
			activeDateInfo = dateInfo.Copy();
		}
		return isActive;
	}
}
