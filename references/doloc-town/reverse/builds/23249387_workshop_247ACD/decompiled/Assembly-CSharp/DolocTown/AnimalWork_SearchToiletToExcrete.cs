using System.Collections.Generic;
using System.Linq;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;

namespace DolocTown;

[Category("环境交互")]
[Name("寻找厕所排泄", 0)]
[Description("优先在当前的房间里寻找厕所进行排泄，如果当前房间没有厕所，则执行失败")]
public class AnimalWork_SearchToiletToExcrete : AnimalWork
{
	public override string Title => "在当前环境中寻找厕所排泄";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		List<IAnimalToilet> list = (from x in animal.CurrentEnv.GetToilets()
			where !x.IsToiletFull
			select x).ToList();
		task = null;
		if (list.Count == 0)
		{
			return false;
		}
		list.Sort((IAnimalToilet a, IAnimalToilet b) => a.Distance(animal.positionCell) - b.Distance(animal.positionCell) + a.AnimalCounter - b.AnimalCounter);
		foreach (IAnimalToilet toilet in list)
		{
			if (AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, toilet.AnimalInteractablePosition, animal.width, out var path))
			{
				task = LinearTask.WaitFrames(1).BeginBreaker(() => !toilet.AnimalInteractableIsValid || toilet.IsToiletFull).AnimalMoveTaskSequence(path)
					.AnimalExcrete(toilet)
					.EndBreaker()
					.Wait(1f);
				return true;
			}
		}
		task = null;
		return false;
	}
}
