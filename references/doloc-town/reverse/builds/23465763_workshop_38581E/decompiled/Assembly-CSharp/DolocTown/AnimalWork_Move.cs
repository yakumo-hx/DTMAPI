using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[Name("移动任务", 0)]
public class AnimalWork_Move : AnimalWork
{
	private enum TargetType
	{
		Around,
		Random
	}

	[SerializeField]
	private TargetType moveTarget;

	public override string Title => "移动到<b>" + TargetLabel + "</b>";

	private string TargetLabel => moveTarget switch
	{
		TargetType.Around => "附近位置", 
		TargetType.Random => "随机位置", 
		_ => "当前位置", 
	};

	private static Vector2Int GetTargetPosition(Animal animal, TargetType type)
	{
		return type switch
		{
			TargetType.Around => animal.AroundPosition, 
			TargetType.Random => animal.RandomPosition, 
			_ => animal.positionCell, 
		};
	}

	public override bool GenTask(Animal animal, out LinearTask task)
	{
		Vector2Int targetPosition = GetTargetPosition(animal, moveTarget);
		return animal.GenTask_JourneyToPosition(targetPosition, out task);
	}
}
