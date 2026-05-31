using System.Collections.Generic;
using DolocTown.Config.Settings;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class WeatherRendererController
{
	private readonly Dictionary<WeatherType, WeatherRenderer> renderers = new Dictionary<WeatherType, WeatherRenderer>();

	public WeatherRenderer currentRenderer { get; private set; }

	public void Init()
	{
		DolocAPI.RegisterMsgListener(UserSettingType.GRAPHICS_SCORCH_SUN_FILTER, OnScorchSunFilterChanged);
	}

	private void OnScorchSunFilterChanged(object o, GameEventArgs args)
	{
		if (currentRenderer is WeatherRenderer_ScorchSun)
		{
			bool value = (bool)((GameEventArgs<object>)args).value;
			DolocAPI.ppm.SetEnabled(PPTypes.HEATWAVE, value);
		}
	}

	public void AddWeatherRenderer(WeatherRenderer renderer)
	{
		if (!renderers.TryAdd(renderer.WeatherType, renderer))
		{
			Debug.LogError($"重复的天气渲染器:{renderer.WeatherType}");
		}
	}

	public T GetRenderer<T>(WeatherType type) where T : WeatherRenderer
	{
		if (renderers.TryGetValue(type, out var value))
		{
			return value as T;
		}
		return null;
	}

	public void Clear()
	{
		if (!(currentRenderer == null))
		{
			currentRenderer.SetEnable(value: false, 0f, transit: false);
			currentRenderer = null;
		}
	}

	public void OnEnterRoom(Room room)
	{
		if (currentRenderer != null)
		{
			currentRenderer.OnEnterRoom(room);
		}
	}

	public void PlayWeather(float process, WeatherType type, bool shouldTransit)
	{
		if (currentRenderer != null)
		{
			if (type == currentRenderer.WeatherType)
			{
				currentRenderer.UpdateStatus(process, shouldTransit);
				return;
			}
			currentRenderer.SetEnable(value: false, process, shouldTransit);
		}
		if (renderers.TryGetValue(type, out var value))
		{
			currentRenderer = value;
			value.SetEnable(value: true, process, shouldTransit);
		}
	}

	public WeatherRenderInfoSO GetRenderInfo(WeatherType type)
	{
		if (!renderers.TryGetValue(type, out var value))
		{
			return null;
		}
		return value.RenderInfo;
	}
}
