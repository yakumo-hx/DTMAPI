using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;

namespace DolocTown;

[Category("专属任务")]
[Name("建造鸡窝", 0)]
[Description("在建筑内找到一个合适的位置建造一个鸡窝")]
public class AnimalWork_BuildChickenNest : AnimalWork
{
	public override string Title => "建造鸡窝";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		task = null;
		return true;
	}
}
