namespace DolocTown.GameData;

public readonly struct DateConfig
{
	public readonly int TULength;

	public readonly int TU2Min;

	public readonly int Hour2Min;

	public readonly int Day2Hour;

	public readonly int Month2Day;

	public readonly int Year2Month;

	public DateConfig(int TULength, int TU2Min, int Hour2Min, int Day2Hour, int Month2Day, int Year2Month)
	{
		this.TULength = TULength;
		this.TU2Min = TU2Min;
		this.Hour2Min = Hour2Min;
		this.Day2Hour = Day2Hour;
		this.Month2Day = Month2Day;
		this.Year2Month = Year2Month;
	}
}
