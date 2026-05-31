using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("环境改造器总能量检查", 0)]
[Description("检查环境改造器的总能量的状态")]
public class DialogueTask_EnvOptimizerEnergy : DialogueConditionTask
{
	[SerializeField]
	private int _energy;

	[SerializeField]
	private CompareMethod _method;

	public override string taskTitle => $"改造器总能量{_method.CompareLabel()} {_energy}";

	protected override bool CheckCondition()
	{
		return OperationTools.Compare(DolocAPI.archiveHandle.farmData.envOptimizerSystem.TotalPower, _energy, _method);
	}
}
