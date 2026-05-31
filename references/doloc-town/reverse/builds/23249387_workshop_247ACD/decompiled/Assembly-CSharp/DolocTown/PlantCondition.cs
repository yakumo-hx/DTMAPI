using DolocTown.Config.Plant;
using DolocTown.Config.Weather;

namespace DolocTown;

public readonly struct PlantCondition
{
	public readonly int month;

	public readonly WeatherType weatherType;

	public readonly bool ignoreSeason;

	public readonly bool ignoreSeasonFungus;

	public readonly bool isInSafeHouse;

	public readonly SeedTypeInfo seedTypeInfo;

	public PlantCondition(SeedTypeInfo seedTypeInfo, int month, WeatherType weatherType, bool ignoreSeason, bool ignoreSeasonFungus, bool isInSafeHouse)
	{
		this.seedTypeInfo = seedTypeInfo;
		this.month = month;
		this.weatherType = weatherType;
		this.ignoreSeason = ignoreSeason;
		this.ignoreSeasonFungus = ignoreSeasonFungus;
		this.isInSafeHouse = isInSafeHouse;
	}

	public bool IsMatchSeed(ItemSeed seed)
	{
		if (seed.seedProto.SeedType != seedTypeInfo.Id)
		{
			return false;
		}
		if (!seed.CheckSeasonValid(month, ignoreSeason, ignoreSeasonFungus))
		{
			return false;
		}
		if (!isInSafeHouse)
		{
			return seed.CanSurviveInWeather(weatherType);
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is PlantCondition plantCondition))
		{
			return false;
		}
		if (month == plantCondition.month && weatherType == plantCondition.weatherType && ignoreSeason == plantCondition.ignoreSeason && ignoreSeasonFungus == plantCondition.ignoreSeasonFungus && isInSafeHouse == plantCondition.isInSafeHouse)
		{
			return seedTypeInfo == plantCondition.seedTypeInfo;
		}
		return false;
	}
}
