using Newtonsoft.Json;

namespace DolocTown.GameData;

public class UnlockDataCollection
{
	[JsonProperty]
	public bool isDisabledSoftMalignantWeather;

	[JsonProperty]
	public bool isDisabledHardMalignantWeather;

	[JsonProperty]
	public bool isUnlockedBetterWater;

	[JsonProperty]
	public bool isUnlockedResonator;

	[JsonProperty]
	public bool isUnlockedBuildingLinkGate;

	[JsonProperty]
	public bool isUnlockedCalendar;

	[JsonProperty]
	public bool isUnlockedAdditionalPassiveSlot;

	[JsonConstructor]
	public UnlockDataCollection(bool isDisabledSoftMalignantWeather = false, bool isDisabledHardMalignantWeather = false, bool isUnlockedBetterWater = false, bool isUnlockedResonator = false, bool isUnlockedBuildingLinkGate = false, bool isUnlockedCalendar = false, bool isUnlockedAdditionalPassiveSlot = false)
	{
		this.isDisabledSoftMalignantWeather = isDisabledSoftMalignantWeather;
		this.isDisabledHardMalignantWeather = isDisabledHardMalignantWeather;
		this.isUnlockedBetterWater = isUnlockedBetterWater;
		this.isUnlockedResonator = isUnlockedResonator;
		this.isUnlockedBuildingLinkGate = isUnlockedBuildingLinkGate;
		this.isUnlockedCalendar = isUnlockedCalendar;
		this.isUnlockedAdditionalPassiveSlot = isUnlockedAdditionalPassiveSlot;
	}
}
