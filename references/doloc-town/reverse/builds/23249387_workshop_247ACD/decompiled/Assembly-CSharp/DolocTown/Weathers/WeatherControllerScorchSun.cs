using DolocTown.Config.Weather;
using UnityEngine;

namespace DolocTown.Weathers;

[WeatherController(WeatherType.SCORCH_SUN)]
public class WeatherControllerScorchSun : WeatherController
{
	public override void OnStart()
	{
	}

	public override void UpdatePerTU(bool shouldRender)
	{
		if (shouldRender)
		{
			RaiseFireEffectsInRandomPosition(DolocAPI.CurrentRoom);
		}
	}

	private void RaiseFireEffectsInRandomPosition(Room room)
	{
		if (!DolocAPI.archiveHandle.ShouldLightUp && room != null && !room.IsInHouse)
		{
			DolocAPI.RaiseInstantPSEffects(room.Geometry.GetRandomSurfacePosition(new Vector2(Random.value, 0f)), InstantParticleEffectsType.FIRE_SPARKS);
		}
	}
}
