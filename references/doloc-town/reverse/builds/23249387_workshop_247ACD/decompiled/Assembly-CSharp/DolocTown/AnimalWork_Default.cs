using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Name("等待", 0)]
public class AnimalWork_Default : AnimalWork
{
	[SerializeField]
	private int waitFrames;

	public override string Title => $"等待{waitFrames}秒";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		task = LinearTask.WaitFrames(waitFrames);
		return true;
	}
}
