using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;

namespace DolocTown;

[Category("专属任务")]
[Name("寻找鸡窝下蛋", 0)]
[Description("在当前房间内寻找合适的鸡窝下蛋")]
public class AnimalWork_SearchChickenNestToProduce : AnimalWork
{
	public override string Title => "寻找鸡窝下蛋";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		task = null;
		return false;
	}
}
