using System;
using RedSaw.AI.LinearTask;

namespace DolocTown.GameData;

[Serializable]
public abstract class NpcScheduleWork
{
	public static NpcScheduleWorkStreet Street = new NpcScheduleWorkStreet();

	protected static LinearTask IntervalTask => new LinearTaskWait(DolocAPI.GlobalParameter.TULength);

	public abstract NpcScheduleWorkType Type { get; }

	public static NpcScheduleWork GetWork(NpcScheduleWorkType type)
	{
		return type switch
		{
			NpcScheduleWorkType.Street => Street, 
			NpcScheduleWorkType.Work => new NpcScheduleWorkWork(), 
			NpcScheduleWorkType.Rest => new NpcScheduleWorkRest(), 
			NpcScheduleWorkType.FellResource => new NpcScheduleWorkFellResource(), 
			NpcScheduleWorkType.Plant => new NpcScheduleWorkPlant(), 
			NpcScheduleWorkType.Relax => new NpcScheduleWorkRelax(), 
			_ => Street, 
		};
	}

	protected static LinearTask Wait(int seconds)
	{
		return new LinearTaskWait(seconds);
	}

	public abstract LinearTask CreateTask(Npc npc);
}
