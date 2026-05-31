using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class EnvCovariantController
{
	private readonly GameObject container;

	private readonly WeatherRendererController weatherRendererController;

	private readonly DayNightRendererController dayNightRendererController;

	private readonly DepthFogController _depthFogController;

	private readonly DepthFogControllerBuilding _depthFogControllerBuilding;

	private readonly CloudShadowController _cloudShadowController;

	public CloudShadowController CloudShadowController => _cloudShadowController;

	public EnvCovariantController(GameObject container, DayNightRendererController dayNightRendererController, WeatherRendererController weatherRendererController)
	{
		this.container = container;
		this.dayNightRendererController = dayNightRendererController;
		this.weatherRendererController = weatherRendererController;
		this.weatherRendererController.Init();
		_depthFogController = container.GetComponentInChildren<DepthFogController>(includeInactive: true);
		_depthFogController.Init();
		_cloudShadowController = container.GetComponentInChildren<CloudShadowController>(includeInactive: true);
		_cloudShadowController.Init();
		_depthFogControllerBuilding = container.GetComponentInChildren<DepthFogControllerBuilding>(includeInactive: true);
		_depthFogControllerBuilding.Init();
	}

	public void OnEnterRoom(Room room, WeatherType weatherType, float dayProcess)
	{
		_depthFogController.SetInRoom(room.IsInHouse);
		_depthFogControllerBuilding.SetInRoom(room.IsInHouse, room.ShouldShowBackground);
		_cloudShadowController.OnEnterRoom(room);
		weatherRendererController.OnEnterRoom(room);
		TransitSun(dayProcess, weatherType, room, 0f);
	}

	public void SetFogEnabled(bool value)
	{
		_depthFogController.SetEnabled(value);
		_depthFogControllerBuilding.SetEnabled(value);
	}

	public void SetFogDensityFullRange(bool transit, float duration = 5f)
	{
		int lv = Random.Range(0, 3);
		_depthFogController.SetDensity(lv, transit, duration);
		_depthFogControllerBuilding.SetDensity(lv, transit, duration);
	}

	public void SetCurrentWeatherRendererEnabled(bool value, float process, bool transit)
	{
		weatherRendererController.currentRenderer?.SetEnable(value, process, transit);
	}

	public void SetFogDensity(int level, bool transit, float duration = 5f)
	{
		_depthFogController.SetDensity(level, transit, duration);
		_depthFogControllerBuilding.SetDensity(level, transit, duration);
	}

	public void ClearFogDensity(bool shouldTransit, float duration = 5f)
	{
		_depthFogController.ClearDensity(shouldTransit, duration);
		_depthFogControllerBuilding.ClearDensity(shouldTransit, duration);
	}

	public void RenderWeather(float process, WeatherType weatherType, Room room, bool transit = true)
	{
		if (!container.activeSelf)
		{
			container.SetActive(value: true);
		}
		weatherRendererController.PlayWeather(process, weatherType, transit);
		WeatherRenderInfoSO renderInfo = weatherRendererController.GetRenderInfo(weatherType);
		if (renderInfo == null)
		{
			DolocAPI.outputError($"无法获取天气渲染信息\"{weatherType}\"");
			return;
		}
		DayNightRenderSettings settings = new DayNightRenderSettings(renderInfo.bloomThreshold, renderInfo.bloomIntensity, renderInfo.vignetteIntensity, renderInfo.vignetteSmoothness, renderInfo.highlightColor, renderInfo.shadowColor, renderInfo.saturation.Evaluate(process));
		dayNightRendererController.TransitPPM(settings, transit);
		TransitSun(process, weatherType, room, transit ? DolocAPI.GlobalParameter.WeatherTransitDuration : 0f);
	}

	public void TransitSun(float process, WeatherType weatherType, Room room, float transitDuration)
	{
		if (room == null)
		{
			return;
		}
		WeatherRenderInfoSO renderInfo = weatherRendererController.GetRenderInfo(weatherType);
		if (renderInfo == null)
		{
			DolocAPI.outputError($"无法获取天气渲染信息\"{weatherType}\"");
			return;
		}
		float num = renderInfo.sunIntensity + DolocAPI.userSettings.sunIntensityAdder;
		Color sunColor;
		if (room.IsInHouse && room.HasActiveLamp)
		{
			sunColor = DolocAPI.GlobalParameter.EnvLightDefaultColor;
			num += ((IEquipmentHost)room).GetLightIntensityAdder();
		}
		else
		{
			sunColor = renderInfo.sunColorGradient.Evaluate(process);
		}
		dayNightRendererController.TransitSun(sunColor, num, transitDuration);
	}

	public void Clear()
	{
		weatherRendererController.Clear();
		_depthFogController.SetEnabled(value: false);
		_cloudShadowController.Disable();
		container.SetActive(value: false);
	}
}
