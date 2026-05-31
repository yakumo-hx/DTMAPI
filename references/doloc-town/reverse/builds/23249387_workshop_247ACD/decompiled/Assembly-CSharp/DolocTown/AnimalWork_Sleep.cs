using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Name("睡眠", 0)]
public class AnimalWork_Sleep : AnimalWork
{
	public override string Title => "睡觉";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		Debug.Log("小动物睡觉任务生成函数");
		task = LinearTask.DoAction(animal.Sleep);
		return true;
	}
}
