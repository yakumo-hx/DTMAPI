using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Name("输出", 0)]
public class AnimalWork_Log : AnimalWork
{
	[SerializeField]
	private string content;

	public override string Title => "输出:\"" + content + "\"";

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		task = LinearTask.DoAction(delegate
		{
			Debug.Log(content);
		});
		return true;
	}
}
