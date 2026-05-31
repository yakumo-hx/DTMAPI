using System.Linq;
using DolocTown.Config.Weather;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class TimeAffectableManager
{
	private readonly SceneLightManager _sceneLightManager;

	private readonly SunSpotManager _sunSpotManager;

	private readonly ProjectiveShadowManager _projectiveShadowManager;

	public TimeAffectableManager(GameObject GO, bool findInScene = true)
	{
		_sceneLightManager = new SceneLightManager(GetComponentsCustom<SceneLight>(GO, findInScene));
		_sceneLightManager.UpdateSwitchStatus(isInitial: true);
		float dayProcess = DolocAPI.archiveHandle.DayProcess;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		_sunSpotManager = new SunSpotManager((from x in GetComponentsCustom<SunSpot>(GO, findInScene)
			select x.GetComponent<Light2D>()).ToArray());
		_sunSpotManager.SetTimeInfo(dayProcess, currentWeatherType, isInitial: true);
		_projectiveShadowManager = new ProjectiveShadowManager((from x in GetComponentsCustom<ProjectiveShadowEx>(GO, findInScene)
			select x.GetComponent<SpriteRenderer>()).ToArray());
		_projectiveShadowManager.SetTimeInfo(dayProcess, currentWeatherType, isInitial: true);
	}

	private static T[] GetComponentsCustom<T>(GameObject go, bool findInScene)
	{
		if (!findInScene)
		{
			return go.GetComponentsInChildren<T>(includeInactive: true);
		}
		return go.GetComponentsInScene<T>(includeInactive: true);
	}

	public void AfterTimePass()
	{
		_sceneLightManager.UpdateSwitchStatus(isInitial: true);
		float dayProcess = DolocAPI.archiveHandle.DayProcess;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		_sunSpotManager.SetTimeInfo(dayProcess, currentWeatherType, isInitial: true);
		_projectiveShadowManager.SetTimeInfo(dayProcess, currentWeatherType, isInitial: true);
	}

	public void UpdatePerTu()
	{
		_sceneLightManager.UpdatePerTu();
		_sceneLightManager.UpdateSwitchStatus();
	}

	public void UpdatePerHour(int hour)
	{
		float dayProcess = DolocAPI.archiveHandle.DayProcess;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		_sunSpotManager.SetTimeInfo(dayProcess, currentWeatherType, isInitial: false);
		_projectiveShadowManager.SetTimeInfo(dayProcess, currentWeatherType, isInitial: false);
	}

	public void OnWeatherChanged(WeatherType weather)
	{
		_sceneLightManager.UpdateSwitchStatus();
		float dayProcess = DolocAPI.archiveHandle.DayProcess;
		_sunSpotManager.SetTimeInfo(dayProcess, weather, isInitial: false);
		_projectiveShadowManager.SetTimeInfo(dayProcess, weather, isInitial: false);
	}

	public void Dispose()
	{
		_projectiveShadowManager.Dispose();
		_sceneLightManager.Dispose();
		_sunSpotManager.Dispose();
	}
}
