using System.Collections.Generic;
using System.Linq;
using ParadoxNotion.Design;
using RedSaw;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Category("环境交互")]
[Name("找东西吃", 0)]
[Description("优先在当前的房间里寻找东西吃，如果当前房间没有食物，则尝试去另外的房间")]
public class AnimalWork_SearchFoodToEat : AnimalWork
{
	public override string Title => "在当前环境中觅食";

	private bool FindFoodInCurrentEnv(Animal animal, out LinearTask task)
	{
		List<IFeeder> list = (from x in animal.CurrentEnv.GetFeeders()
			where !x.IsFeederEmpty
			select x).ToList();
		task = null;
		if (list.Count == 0)
		{
			return false;
		}
		foreach (IFeeder feeder in from x in list
			orderby x.FeederPriority descending, x.AnimalInteractablePosition.ManhattenDistance(animal.positionCell) + x.AnimalCounter
			select x)
		{
			Vector2Int feederTouchPosition = feeder.GetFeederTouchPosition(animal.currentRoom, animal.width, animal.positionCell);
			if (AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, feederTouchPosition, animal.width, out var path))
			{
				task = LinearTask.WaitFrames(1).BeginBreaker(() => !feeder.AnimalInteractableIsValid || feeder.IsFeederEmpty).AnimalMoveTaskSequence(path)
					.AnimalEat(feeder)
					.EndBreaker()
					.Wait(1f);
				return true;
			}
		}
		task = null;
		return false;
	}

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		return FindFoodInCurrentEnv(animal, out task);
	}
}
