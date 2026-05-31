using System.Collections.Generic;
using System.Linq;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Name("繁育", 0)]
[Description("在当前环境中寻找空闲的繁育室进行繁育")]
public class AnimalWork_SearchLivestockNurseryToBreed : AnimalWork
{
	public override string Title => "进行繁育";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		Debug.Log("AnimalWork_SearchLivestockNurseryToBreed: 开始繁育逻辑");
		List<IAnimalLivestockNursery> list = (from x in animal.CurrentEnv.GetLivestockNurseries()
			where x.IsLivestockNurseryFree
			select x).ToList();
		task = null;
		if (list.Count == 0)
		{
			return false;
		}
		list.Sort((IAnimalLivestockNursery a, IAnimalLivestockNursery b) => a.Distance(animal.positionCell) - b.Distance(animal.positionCell));
		foreach (IAnimalLivestockNursery nursery in list)
		{
			if (!AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, nursery.AnimalInteractablePosition, animal.width, out var path))
			{
				continue;
			}
			Debug.Log("AnimalWork_SearchLivestockNurseryToBreed: 生成繁育任务");
			task = LinearTask.WaitFrames(1).BeginBreaker(() => !nursery.AnimalInteractableIsValid || !nursery.IsLivestockNurseryFree).AnimalMoveTaskSequence(path)
				.Do(delegate
				{
					if (nursery.IsLivestockNurseryFree && animal._Breed())
					{
						nursery.StartBreed(animal.proto.BreedDuration);
					}
				})
				.EndBreaker()
				.Wait(1f);
			return true;
		}
		task = null;
		return false;
	}
}
