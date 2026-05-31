using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("年份过滤器", 0)]
public class SwitchScheduleNodeFilterYear : SwitchScheduleNodeFilter
{
	[SerializeField]
	[ExposeField]
	private int year;

	[SerializeField]
	[ExposeField]
	private int yearEnd;

	[SerializeField]
	[ExposeField]
	private bool isRange;

	[SerializeField]
	[ExposeField]
	private bool invert;

	public override bool IsMatch(SwitchScheduleParams param)
	{
		int num = param.date.Year;
		if (isRange)
		{
			if (invert)
			{
				if (num >= year)
				{
					return num > yearEnd;
				}
				return true;
			}
			if (num >= year)
			{
				return num <= yearEnd;
			}
			return false;
		}
		if (invert)
		{
			return num != year;
		}
		return num == year;
	}
}
