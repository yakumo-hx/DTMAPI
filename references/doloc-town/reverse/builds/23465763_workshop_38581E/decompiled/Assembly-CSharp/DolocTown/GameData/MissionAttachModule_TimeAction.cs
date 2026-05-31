using UnityEngine;

namespace DolocTown.GameData;

public class MissionAttachModule_TimeAction : MissionAttachModule
{
	public enum TimingType
	{
		Daily,
		Weekly,
		Monthly,
		Yearly
	}

	public enum ActionType
	{
		ClearProgress
	}

	[SerializeField]
	private TimingType _timingType;

	[SerializeField]
	private ActionType _action;

	private static void DoAction(IMission mission, ActionType action)
	{
		if (action == ActionType.ClearProgress)
		{
			mission.ClearProgress();
			if (!mission.IsImplicit)
			{
				DolocAPI.RefreshResidentMissionTip(mission.Id);
			}
		}
	}

	public override void OnDailyRefresh(IMission mission)
	{
		if (_timingType == TimingType.Daily)
		{
			DoAction(mission, _action);
		}
	}

	public override void OnWeeklyRefresh(IMission mission)
	{
		if (_timingType == TimingType.Weekly)
		{
			DoAction(mission, _action);
		}
	}

	public override void OnMonthlyRefresh(IMission mission)
	{
		if (_timingType == TimingType.Monthly)
		{
			DoAction(mission, _action);
		}
	}

	public override void OnYearlyRefresh(IMission mission)
	{
		if (_timingType == TimingType.Yearly)
		{
			DoAction(mission, _action);
		}
	}
}
