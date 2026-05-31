using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("小时过滤器", 0)]
[Description("根据小时过滤，当设为范围时，范围为左闭右开，即包含起始时间，不包含结束时间")]
public class SwitchScheduleNodeFilterHour : SwitchScheduleNodeFilter
{
	[SerializeField]
	[ExposeField]
	private bool invert;

	[SerializeField]
	[ExposeField]
	private bool isRange;

	[SerializeField]
	[ExposeField]
	private int hour;

	[SerializeField]
	[ExposeField]
	private int hourEnd;

	public override bool IsMatch(SwitchScheduleParams npcScheduleParams)
	{
		int num = npcScheduleParams.date.Hour;
		if (invert)
		{
			if (isRange)
			{
				if (num >= hour)
				{
					return num >= hourEnd;
				}
				return true;
			}
			return num != hour;
		}
		if (isRange)
		{
			if (num >= hour)
			{
				return num < hourEnd;
			}
			return false;
		}
		return num == hour;
	}
}
