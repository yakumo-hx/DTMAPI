using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public abstract class WeatherRenderer : DolocObject
{
	public abstract WeatherType WeatherType { get; }

	public abstract WeatherRenderInfoSO RenderInfo { get; }

	protected static void ControlGlobalCloudShadow(bool enable, float process, bool transit, WeatherType WeatherType, Color c)
	{
		ControlGlobalCloudShadow(enable, process, transit, WeatherType);
		if (enable)
		{
			DolocAPI.EnvCovariantController.CloudShadowController.SetShadowColor(c, transit);
		}
	}

	protected static void ControlGlobalCloudShadow(bool enable, float process, bool transit, WeatherType WeatherType)
	{
		CloudShadowController cloudShadowController = DolocAPI.EnvCovariantController.CloudShadowController;
		if (enable)
		{
			cloudShadowController.Enable(!transit);
			cloudShadowController.ShowCloud(process, WeatherType, !transit);
		}
		else
		{
			cloudShadowController.HideCloud(!transit);
		}
		if (!transit)
		{
			cloudShadowController.RefreshPosition();
		}
	}

	public virtual void SetEnable(bool value, float process, bool transit)
	{
	}

	public virtual void OnEnterRoom(Room room)
	{
	}

	public virtual void UpdateStatus(float process, bool transit)
	{
	}
}
