using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("全局天数过滤器", 0)]
public class SwitchScheduleNodeFilterGlobalDay : SwitchScheduleNodeFilter
{
	[SerializeField]
	[ExposeField]
	private bool invert;

	[SerializeField]
	[ExposeField]
	private bool isRange;

	[SerializeField]
	[ExposeField]
	private int day;

	[SerializeField]
	[ExposeField]
	private int dayEnd;

	public override bool IsMatch(SwitchScheduleParams param)
	{
		int totalDays = param.date.TotalDays;
		if (invert)
		{
			if (isRange)
			{
				if (totalDays >= day)
				{
					return totalDays > dayEnd;
				}
				return true;
			}
			return totalDays != day;
		}
		if (isRange)
		{
			if (totalDays >= day)
			{
				return totalDays <= dayEnd;
			}
			return false;
		}
		return totalDays == day;
	}
}
