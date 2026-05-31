using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateTaskFertilizer : AutomateTask
{
	private readonly PlantBasin basin;

	public AutomateTaskFertilizer(PlantBasin basin)
	{
		this.basin = basin;
	}

	public override TaskStatus OnExecute(float dt)
	{
		base.Bot.locker.UnlockEquipment(basin);
		if (basin.IsFertilizerd)
		{
			return TaskStatus.Success;
		}
		ItemFertilizer itemFertilizer = base.Bot.inventory.AutomatePatchTakeItem<ItemFertilizer>();
		if (itemFertilizer == null)
		{
			return TaskStatus.Failure;
		}
		bool isRenderNow = base.Bot.IsRenderNow;
		if (basin.Fertilizer(itemFertilizer, isRenderNow, sendMessage: false, isRenderNow))
		{
			base.Bot.RaiseSpriteFadeUp(itemFertilizer.uiSprite);
			return TaskStatus.Success;
		}
		base.Bot.inventory.PlaceItem(itemFertilizer);
		return TaskStatus.Failure;
	}
}
