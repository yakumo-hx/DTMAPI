using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public struct WeatherHistoryRecord
{
	[JsonProperty]
	public byte weatherType;

	[JsonProperty]
	public int startTime;

	[JsonProperty]
	public int duration;

	[JsonConstructor]
	public WeatherHistoryRecord(byte weatherType, int startTime, int duration)
	{
		this.weatherType = weatherType;
		this.startTime = startTime;
		this.duration = duration;
	}

	public override readonly string ToString()
	{
		return $"{weatherType} [{startTime}, {startTime + duration}]";
	}
}
