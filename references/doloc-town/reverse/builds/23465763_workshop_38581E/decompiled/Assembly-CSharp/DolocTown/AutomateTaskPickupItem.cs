using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateTaskPickupItem : AutomateTask
{
	private readonly DropItemBase dropItem;

	public AutomateTaskPickupItem(DropItemBase dropItem)
	{
		this.dropItem = dropItem;
	}

	public override TaskStatus OnExecute(float dt)
	{
		base.Bot.locker.UnlockDropItem(dropItem);
		if (dropItem.IsRemoved || dropItem.Host == null)
		{
			return TaskStatus.Failure;
		}
		if (!TryGetItem(dropItem, out var item))
		{
			return TaskStatus.Failure;
		}
		if (!dropItem.Host.RemoveDropItem(dropItem))
		{
			return TaskStatus.Failure;
		}
		Item item2 = base.Bot.inventory.PlaceItem(item);
		if (item2 != null)
		{
			DolocAPI.GenerateDropItem(dropItem.Host, item2, dropItem.PositionWS, dropItem.ShouldSendMsg);
		}
		base.Bot.RaiseSpriteFadeUp(dropItem.SceneSprite);
		return TaskStatus.Success;
	}

	private static bool TryGetItem(DropItemBase dropItem, out Item item)
	{
		if (!(dropItem is DropItem dropItem2))
		{
			if (dropItem is SpecialDropItem specialDropItem)
			{
				item = specialDropItem.DropItem;
				return true;
			}
			item = null;
			return false;
		}
		item = DolocAPI.GenerateItem(dropItem2.ItemName);
		return true;
	}
}
