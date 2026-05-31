using DG.Tweening;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class WeatherRenderer_AcidRain : WeatherRenderer
{
	[SerializeField]
	private ParticleSystem ps;

	[SerializeField]
	private WeatherRenderInfoSO renderInfo;

	[SerializeField]
	[Range(0f, 1f)]
	private float heavyProbility = 0.2f;

	[SerializeField]
	[Range(0f, 1f)]
	private float objectHeavyProbility = 0.3f;

	[SerializeField]
	[Range(0f, 1f)]
	private float waterWaveProbility = 0.15f;

	[SerializeField]
	private Gradient particleColor;

	[SerializeField]
	[Range(0f, 100f)]
	private int particleEmission = 10;

	[SerializeField]
	[Range(0f, 2f)]
	private float particleLength = 0.5f;

	private RaindropCollision[] raindropCollisions;

	public override WeatherType WeatherType => WeatherType.ACID_RAIN;

	public override WeatherRenderInfoSO RenderInfo => renderInfo;

	protected override void __Init()
	{
		base.__Init();
		raindropCollisions = GetComponentsInChildren<RaindropCollision>(includeInactive: true);
		RaindropCollision[] array = raindropCollisions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Init(heavyProbility, objectHeavyProbility, waterWaveProbility);
		}
	}

	public override void SetEnable(bool enable, float dayProcess, bool transit)
	{
		SetVisible(enable);
		if (enable)
		{
			StartParticleSystems(DolocAPI.CurrentRoom?.IsInHouse ?? false);
			ApplyParticleSystemProperties(ps);
			bool value = !(DolocAPI.archiveHandle.currentRoom?.IsInHouse ?? false);
			GlobalFogMateiralProperties component = GetComponent<GlobalFogMateiralProperties>();
			component.ApplyMaterialProperties(DolocAPI.ppm.MaterialGlobalFog);
			DolocAPI.ppm.SetEnabled(PPTypes.GLOBALFOG, value);
			DolocAPI.ppm.GlobalFog.Play(component.Intensity, 1f);
			DolocAPI.EnvCovariantController.SetFogEnabled(value: true);
			DolocAPI.EnvCovariantController.SetFogDensityFullRange(transit);
			Room currentRoom = DolocAPI.CurrentRoom;
			SetShouldCheckRainDropPosition(currentRoom == null || !currentRoom.IsInHouse);
		}
		else
		{
			ps.Stop(withChildren: true);
			DolocAPI.ppm.GlobalFog.Play(0f, 1f, Ease.Linear, delegate
			{
				DolocAPI.ppm.SetEnabled(PPTypes.GLOBALFOG, value: false);
			});
			DolocAPI.EnvCovariantController.ClearFogDensity(transit);
		}
	}

	private void SetShouldCheckRainDropPosition(bool value)
	{
		RaindropCollision[] array = raindropCollisions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ShouldCheckPosition = value;
		}
	}

	private void ApplyParticleSystemProperties(ParticleSystem ps)
	{
		ParticleSystem.MainModule main = ps.main;
		ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
		ParticleSystem.EmissionModule emission = ps.emission;
		main.startSizeY = particleLength;
		colorOverLifetime.color = particleColor;
		emission.rateOverTime = particleEmission;
	}

	public override void OnEnterRoom(Room room)
	{
		if (room == null)
		{
			return;
		}
		RaindropCollision[] array;
		if (room.IsInHouse)
		{
			DolocAPI.ppm.SetEnabled(PPTypes.GLOBALFOG, value: false);
			SetShouldCheckRainDropPosition(value: false);
			array = raindropCollisions;
			foreach (RaindropCollision raindropCollision in array)
			{
				if (raindropCollision.disableInRoom)
				{
					raindropCollision.ps.Stop();
				}
			}
			return;
		}
		DolocAPI.ppm.SetEnabled(PPTypes.GLOBALFOG, value: true);
		SetShouldCheckRainDropPosition(value: true);
		array = raindropCollisions;
		foreach (RaindropCollision raindropCollision2 in array)
		{
			if (!raindropCollision2.ps.isPlaying)
			{
				raindropCollision2.ps.Play();
			}
		}
	}

	private void StartParticleSystems(bool isInhouse)
	{
		RaindropCollision[] array = raindropCollisions;
		foreach (RaindropCollision raindropCollision in array)
		{
			if (!(raindropCollision.disableInRoom && isInhouse))
			{
				raindropCollision.ps.Play();
			}
		}
	}
}
