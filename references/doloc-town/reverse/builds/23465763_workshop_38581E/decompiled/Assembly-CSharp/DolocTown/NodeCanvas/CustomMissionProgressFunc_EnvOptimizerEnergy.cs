using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("环境改造器总能量目标", 0)]
public class CustomMissionProgressFunc_EnvOptimizerEnergy : CustomMissionProgressFunc
{
	[SerializeField]
	private int targetValue;

	public override string GetProgressInfo()
	{
		int num = DolocAPI.archiveHandle?.farmData.envOptimizerSystem.TotalPower ?? 0;
		return SetColor(num >= targetValue, $"{num}/{targetValue}");
	}
}
