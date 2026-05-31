using System.Collections.Generic;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class WeatherRenderer_Rain : WeatherRenderer
{
	[SerializeField]
	private WeatherRenderInfoSO renderInfo;

	[SerializeField]
	[Range(0f, 1f)]
	private float heavyProbility = 0.01f;

	[SerializeField]
	[Range(0f, 1f)]
	private float objectHeavyProbility = 0.3f;

	[SerializeField]
	[Range(0f, 1f)]
	private float waterWaveProbility = 0.15f;

	private RaindropCollision[] raindropCollisions;

	public override WeatherType WeatherType => WeatherType.RAIN;

	public override WeatherRenderInfoSO RenderInfo => renderInfo;

	public override void SetEnable(bool value, float dayProcess, bool transit)
	{
		SetVisible(value);
		if (value)
		{
			StartParticleSystems(DolocAPI.CurrentRoom?.IsInHouse ?? false);
			int level = (RandomUtils.Dice(0.5f) ? 1 : 0);
			DolocAPI.EnvCovariantController.SetFogEnabled(value: true);
			DolocAPI.EnvCovariantController.SetFogDensity(level, transit);
			Room currentRoom = DolocAPI.CurrentRoom;
			SetShouldCheckRainDropPosition(currentRoom == null || !currentRoom.IsInHouse);
		}
		else
		{
			DolocAPI.EnvCovariantController.ClearFogDensity(transit);
		}
	}

	public override void OnEnterRoom(Room room)
	{
		if (room == null)
		{
			return;
		}
		DolocAPI.effectProvider.ClearEntityEffects<RainFogParticle>();
		RaindropCollision[] array;
		if (room.IsInHouse)
		{
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
		List<RaindropCollision> list = new List<RaindropCollision>();
		RaindropCollision[] array = raindropCollisions;
		foreach (RaindropCollision raindropCollision in array)
		{
			if (raindropCollision.disableInRoom && isInhouse)
			{
				list.Add(raindropCollision);
			}
			else
			{
				raindropCollision.ps.Play();
			}
		}
		foreach (RaindropCollision item in list)
		{
			if (item.ps.isPlaying)
			{
				item.ps.Stop();
			}
		}
	}

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

	public void SetShouldCheckRainDropPosition(bool value)
	{
		RaindropCollision[] array = raindropCollisions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ShouldCheckPosition = value;
		}
	}
}
