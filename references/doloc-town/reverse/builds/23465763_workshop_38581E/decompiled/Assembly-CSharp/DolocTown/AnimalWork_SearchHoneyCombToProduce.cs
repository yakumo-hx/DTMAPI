using System.Collections.Generic;
using System.Linq;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Category("专属任务")]
[Name("寻找蜂箱生产", 0)]
[Description("在当前房间寻找未满的蜂箱设备生产蜂蜜")]
public class AnimalWork_SearchHoneyCombToProduce : AnimalWork
{
	[SerializeField]
	private bool shouldLog;

	public override string Title => "寻找蜂箱产出";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		Debug.Log("<color=yellow>寻找蜂箱进行产出</color>");
		task = null;
		List<IAnimalHoneyComb> list = (from x in animal.CurrentEnv.GetHoneyCombs()
			where !x.IsHoneyCombFull
			select x).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		list.Sort((IAnimalHoneyComb a, IAnimalHoneyComb b) => a.Distance(animal.positionCell) - b.Distance(animal.positionCell));
		foreach (IAnimalHoneyComb honeyComb in list)
		{
			if (AnimalUtils.CanArrive(animal.currentRoom, animal.positionCell, honeyComb.AnimalInteractablePosition, animal.width, out var path))
			{
				task = LinearTask.StartWith.AnimalMoveTaskSequence(path).Do(delegate
				{
					Produce(honeyComb, animal);
				}).Wait(1f);
				return true;
			}
		}
		return false;
	}

	private void Produce(IAnimalHoneyComb honeyComb, Animal animal)
	{
		if (honeyComb.AnimalInteractableIsValid && !honeyComb.IsHoneyCombFull && animal.NeedMetabolism)
		{
			if (shouldLog)
			{
				Debug.Log("<color=yellow>产出蜂蜜</color>");
			}
			honeyComb.ProduceHoney(animal.ProduceAsItems());
		}
	}
}
