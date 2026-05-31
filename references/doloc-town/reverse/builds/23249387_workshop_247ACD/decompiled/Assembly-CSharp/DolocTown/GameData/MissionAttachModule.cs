using System;

namespace DolocTown.GameData;

[Serializable]
public abstract class MissionAttachModule
{
	public virtual void OnDailyRefresh(IMission mission)
	{
	}

	public virtual void OnWeeklyRefresh(IMission mission)
	{
	}

	public virtual void OnMonthlyRefresh(IMission mission)
	{
	}

	public virtual void OnYearlyRefresh(IMission mission)
	{
	}
}
