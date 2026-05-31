using DolocTown.Config;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class IndependentWeatherSystem
{
	[JsonProperty]
	public WeatherSystem WeatherSystem;

	[JsonProperty]
	private WeatherMapPatch weatherMapPatch;

	[JsonProperty]
	private int randomSeed;

	private WeatherMapGenerator _weatherMapGenerator;

	public WeatherInfo CurrentWeatherInfo => WeatherSystem.WeatherInfo;

	public WeatherType CurrentWeatherType => WeatherSystem.WeatherType;

	public SeasonInfo CurrentSeasonInfo { get; private set; }

	public IndependentWeatherSystem(WeatherType weatherType, int[] months)
	{
		WeatherSystem = new WeatherSystem(weatherType);
		weatherMapPatch = new WeatherMapPatch();
		randomSeed = Random.Range(0, int.MaxValue);
		_weatherMapGenerator = new WeatherMapGenerator(randomSeed, months);
	}

	[JsonConstructor]
	protected IndependentWeatherSystem(WeatherSystem weatherSystem, WeatherMapPatch weatherMapPatch, int randomSeed)
	{
		WeatherSystem = weatherSystem;
		this.weatherMapPatch = weatherMapPatch;
		this.randomSeed = randomSeed;
		_weatherMapGenerator = new WeatherMapGenerator(randomSeed);
	}

	public void RetrieveSeasonProto(int month)
	{
		int month2 = _weatherMapGenerator.GetMonth(month);
		CurrentSeasonInfo = DolocConfig.Tables.TbSeason.GetByMonth(month2);
	}
}
