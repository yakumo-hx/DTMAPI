using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;

namespace DolocTown;

[Name("醒来", 0)]
[Description("如果小动物正在睡觉，则恢复到正常状态")]
public class AnimalWork_WakeUp : AnimalWork
{
	public override string Title => "醒来";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		task = LinearTask.DoAction(animal.WakeUp);
		return true;
	}
}
