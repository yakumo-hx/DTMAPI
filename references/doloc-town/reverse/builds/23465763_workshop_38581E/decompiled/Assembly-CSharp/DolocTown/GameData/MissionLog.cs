namespace DolocTown.GameData;

public readonly struct MissionLog
{
	public readonly string time;

	public readonly bool isImportant;

	public readonly string log;

	public MissionLog(bool isImportant, string log, string time)
	{
		this.isImportant = isImportant;
		this.log = log;
		this.time = time;
	}

	public static implicit operator string(MissionLog log)
	{
		return log.log;
	}

	public override string ToString()
	{
		return log;
	}
}
