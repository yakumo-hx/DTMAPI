using System.Collections.Generic;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AutomateTaskGatherEquipment : AutomateTask
{
	private readonly Equipment _equipment;

	public AutomateTaskGatherEquipment(Equipment equipment)
	{
		_equipment = equipment;
	}

	public override TaskStatus OnExecute(float dt)
	{
		base.Bot.locker.UnlockEquipment(_equipment);
		if (!(_equipment is IGatherableEquipment gatherableEquipment))
		{
			return TaskStatus.Failure;
		}
		Item[] array = gatherableEquipment.Gather();
		List<Sprite> list = new List<Sprite>();
		Item[] array2 = array;
		foreach (Item item in array2)
		{
			Item item2 = base.Bot.inventory.PlaceItem(item);
			if (item2 != null)
			{
				DolocAPI.GenerateDropItem(_equipment.CurrentRoom, item2, _equipment.Position);
			}
			else
			{
				list.Add(item.uiSprite);
			}
		}
		base.Bot.RaiseSpriteArrayFadeUp(list.ToArray());
		return TaskStatus.Success;
	}
}
