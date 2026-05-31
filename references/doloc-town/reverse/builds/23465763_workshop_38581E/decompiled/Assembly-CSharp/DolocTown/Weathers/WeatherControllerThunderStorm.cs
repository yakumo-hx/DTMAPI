using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Weather;
using RedSaw;
using UnityEngine;

namespace DolocTown.Weathers;

[WeatherController(WeatherType.THUNDERSTORM)]
public class WeatherControllerThunderStorm : WeatherController
{
	private readonly Counter _flashCounter = new Counter();

	private Coroutine _thunderCoroutine;

	private void ResetFlashCounter()
	{
		_flashCounter.SetInterval(Random.Range(DolocAPI.GlobalParameter.ThunderFrequency.x, DolocAPI.GlobalParameter.ThunderFrequency.y));
	}

	public override void OnStart()
	{
		ResetFlashCounter();
	}

	public override void UpdatePerTU(bool shouldRender)
	{
		if (_flashCounter.Tick())
		{
			ResetFlashCounter();
			Thunder(shouldRender);
		}
	}

	private void RaiseThunderEffects(Vector2 position, Equipment targetEquipment, bool raiseObjectThunder)
	{
		DolocAPI.RaiseInstantAnimEffects(position, InstAnimEffectType.THUNDER, LocMaterials.GAME_MAT_THUNDER);
		if (raiseObjectThunder && _thunderCoroutine == null)
		{
			_thunderCoroutine = DolocAPI.Delay(0.4f, delegate
			{
				if (targetEquipment.IsRender)
				{
					DolocAPI.RaiseInstantAnimEffects(position, InstAnimEffectType.ELECTRIC_CURRENT, LocMaterials.GAME_MAT_THUNDER);
				}
				_thunderCoroutine = null;
			});
		}
		DolocAPI.RaiseInstantPSEffects(position, InstantParticleEffectsType.ELECTRIC_SPARKS);
		DolocAPI.RaiseInstantPSEffects(position, InstantParticleEffectsType.LIGHT_SMOKE);
		DolocAPI.cameraController.ShakeScreen();
	}

	private void ThunderToEquipment(Room farmRoom, bool shouldRender)
	{
		Equipment randomEquipment = farmRoom.DM_equipment.GetRandomEquipment();
		if (randomEquipment != null)
		{
			Equipment equipment = randomEquipment.Thunder(shouldRender && farmRoom.isRenderNow);
			bool flag = equipment is LightningArrester;
			Vector3 vector = (flag ? equipment.PositionTop : equipment.PositionCenter);
			if (farmRoom.isRenderNow)
			{
				RaiseThunderEffects(vector, equipment, flag || RandomUtils.Dice(0.3f));
			}
		}
	}

	private void ThunderToBuilding(Room farmRoom, bool shouldRender)
	{
		Building randomBuilding = farmRoom.DM_building.GetRandomBuilding();
		if (randomBuilding == null)
		{
			return;
		}
		Vector2Int[] coveredPositions = randomBuilding.CoveredPositions;
		LightningArrester[] equipments = ((IEquipmentHost)farmRoom).GetEquipments<LightningArrester>();
		foreach (LightningArrester lightningArrester in equipments)
		{
			if (lightningArrester.IsCoverPositions(coveredPositions))
			{
				lightningArrester.OnThunder(farmRoom.isRenderNow);
				if (farmRoom.isRenderNow)
				{
					RaiseThunderEffects(lightningArrester.PositionTop, lightningArrester, raiseObjectThunder: true);
				}
				return;
			}
		}
		randomBuilding.Damage(randomBuilding.proto.DamageSufferRateThunder * DolocAPI.GlobalParameter.ThunderDamgeToBuilding);
		if (farmRoom.isRenderNow)
		{
			RaiseThunderEffects(randomBuilding.PositionTop, null, raiseObjectThunder: false);
		}
	}

	private void ThunderToAnimal(Room farmRoom, bool shouldRender)
	{
		IEnumerable<Animal> enumerable = farmRoom.animalSystem.Animals.Where((Animal x) => !x.IsInHouse);
		Animal[] array = (enumerable as Animal[]) ?? enumerable.ToArray();
		if (!array.Any())
		{
			return;
		}
		Animal animal = array.Choice();
		Vector2Int[] positions = animal.CoverPositions.ToArray();
		LightningArrester[] equipments = ((IEquipmentHost)farmRoom).GetEquipments<LightningArrester>();
		foreach (LightningArrester lightningArrester in equipments)
		{
			if (lightningArrester.IsCoverPositions(positions))
			{
				lightningArrester.OnThunder(farmRoom.isRenderNow);
				if (shouldRender)
				{
					RaiseThunderEffects(lightningArrester.PositionTop, lightningArrester, raiseObjectThunder: true);
				}
				return;
			}
		}
		animal.OnThunder();
		if (animal.IsRender)
		{
			RaiseThunderEffects(animal.PositionWSOfCell, null, raiseObjectThunder: false);
		}
	}

	private void Thunder(bool shouldRender)
	{
		TemplateRoomOutdoor mainFarm = DolocAPI.archiveHandle.farmData.MainFarm;
		if (RandomUtils.Dice(0.3f))
		{
			ThunderToBuilding(mainFarm, shouldRender);
		}
		else
		{
			ThunderToEquipment(mainFarm, shouldRender);
		}
		if (RandomUtils.Dice(DolocAPI.GlobalParameter.AnimalThunderProbability))
		{
			ThunderToAnimal(mainFarm, shouldRender);
		}
		if (shouldRender)
		{
			DolocAPI.RaiseInstantAnimEffects(Vector2.zero, InstAnimEffectType.GLOBAL_FLASH);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_LIGHTNING);
		}
	}
}
