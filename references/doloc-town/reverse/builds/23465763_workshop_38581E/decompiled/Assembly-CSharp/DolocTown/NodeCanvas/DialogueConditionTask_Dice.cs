using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("骰子", 0)]
[Description("以给定的概率产生一个true值")]
public class DialogueConditionTask_Dice : DialogueConditionTask
{
	public enum PlayerStatus
	{
		Health,
		Energy
	}

	[SerializeField]
	[SliderField(0, 1)]
	public float probability = 1f;

	private string percentStr => (probability * 100f).ToString("F0");

	public override string taskTitle => percentStr + "%概率";

	protected override bool CheckCondition()
	{
		return Random.value <= probability;
	}
}
