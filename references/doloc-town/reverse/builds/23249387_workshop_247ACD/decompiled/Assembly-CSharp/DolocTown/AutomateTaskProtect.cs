using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateTaskProtect : AutomateTask
{
	private readonly PlantBasin basin;

	public AutomateTaskProtect(PlantBasin basin)
	{
		this.basin = basin;
	}

	public override TaskStatus OnExecute(float dt)
	{
		base.Bot.locker.UnlockEquipment(basin);
		if (basin.IsProtected)
		{
			return TaskStatus.Success;
		}
		ItemFilm itemFilm = base.Bot.inventory.AutomatePatchTakeItem<ItemFilm>();
		if (itemFilm == null)
		{
			return TaskStatus.Failure;
		}
		if (basin.Protect(itemFilm, base.Bot.IsRenderNow))
		{
			base.Bot.RaiseSpriteFadeUp(itemFilm.uiSprite, fadeUp: false);
			return TaskStatus.Success;
		}
		base.Bot.inventory.PlaceItem(itemFilm);
		return TaskStatus.Failure;
	}
}
