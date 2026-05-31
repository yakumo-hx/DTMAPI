using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown.GameData;

public class NpcScheduleWorkPlant : NpcScheduleWork
{
	private enum SeedFlag
	{
		Id,
		List,
		Lut
	}

	[SerializeField]
	private SeedFlag seedFlag;

	[SerializeField]
	private string seedId;

	[SerializeField]
	private List<string> seedIds;

	[SerializeField]
	private string seedLutName;

	[SerializeField]
	private string plantBasinName;

	[SerializeField]
	private int roomMaxPlant;

	public override NpcScheduleWorkType Type => NpcScheduleWorkType.Plant;

	public override LinearTask CreateTask(Npc npc)
	{
		LinearTask result = NpcScheduleWork.Wait(10);
		if (!npc.TryGetCurrentRoom(out var room))
		{
			return result;
		}
		PlantBasin equipment = ((IEquipmentHost)room).GetEquipment((Func<PlantBasin, bool>)((PlantBasin x) => x.Crop == null));
		if (equipment == null)
		{
			if (((IEquipmentHost)room).CountEquipment<PlantBasin>() >= roomMaxPlant)
			{
				return result;
			}
			if (!TryBuildPlantBasin(room, out var task))
			{
				return result;
			}
			return task;
		}
		if (!TryGetSeedProto(out var seed))
		{
			return result;
		}
		ItemSeed seed2 = (ItemSeed)DolocAPI.GenerateItem(seed.Id);
		return PlantSeed(seed2, equipment);
	}

	private bool TryGetSeedProto(out SeedInfo seed)
	{
		switch (seedFlag)
		{
		case SeedFlag.Id:
			seed = DolocConfig.Tables.TbSeed.GetOrDefault(seedId);
			return seed != null;
		case SeedFlag.List:
			seed = DolocConfig.Tables.TbSeed.GetOrDefault(seedIds[UnityEngine.Random.Range(0, seedIds.Count)]);
			return seed != null;
		case SeedFlag.Lut:
		{
			ItemSpawnInfo orDefault = DolocConfig.Tables.TbItemSpawn.GetOrDefault(seedLutName);
			if (orDefault == null)
			{
				seed = null;
				return false;
			}
			CountItem countItem = orDefault.SpawnItems(1)[0];
			seed = DolocConfig.Tables.TbSeed.GetOrDefault(countItem.itemName);
			return seed != null;
		}
		default:
			seed = null;
			return false;
		}
	}

	private LinearTask PlantSeed(ItemSeed seed, PlantBasin basin)
	{
		return NpcScheduleWork.Wait(1).NpcMove(basin.Position.x).Wait(1f)
			.NpcDo(delegate(Npc npc)
			{
				basin.Plant(seed, npc.IsRenderNow);
				if (npc.IsRenderNow)
				{
					npc.Renderer.PlayAnimation("interact", force: false);
					ItemInfo orDefault = DolocConfig.Tables.TbItem.GetOrDefault(seed.name);
					if (orDefault != null)
					{
						DolocAPI.RaiseSpriteFadeUp(basin.PositionTop, orDefault.UiSpriteAsset.Asset);
					}
				}
			})
			.Wait(1f);
	}

	private bool TryBuildPlantBasin(Room room, out LinearTask task)
	{
		task = null;
		if (plantBasinName.IsNullOrEmpty())
		{
			return false;
		}
		EquipmentInfo orDefault = DolocConfig.Tables.TbEquipment.GetOrDefault(plantBasinName);
		if (orDefault == null)
		{
			return false;
		}
		if (!((IEquipmentHost)room).GetRandomEmptyBuildPosition(orDefault.CoverSize, out Vector2Int position))
		{
			return false;
		}
		task = BuildPlantBasin(room, orDefault, position);
		return true;
	}

	private LinearTask BuildPlantBasin(IEquipmentHost host, EquipmentInfo proto, Vector2Int pos)
	{
		Vector2 vector = new Vector2((float)proto.CoverSize.x * 0.5f, 0f) + pos;
		Vector2 positionWS = vector * DolocTransform.TILE_WORLD_SIZE + host.CurrentRoom.RoomPosition;
		return NpcScheduleWork.Wait(1).NpcMove(positionWS.x).Wait(1f)
			.NpcDo(delegate(Npc npc)
			{
				if (npc.IsRenderNow)
				{
					npc.Renderer.PlayAnimation("interact", force: false);
					DolocAPI.Delay(0.5f, delegate
					{
						if (npc.IsRenderNow)
						{
							host.CreateEquipment(positionWS, pos, proto, turn: false);
							DolocAPI.cameraController.ShakeScreen(0.1f, 0.2f);
							DolocAPI.RaiseInstantAnimEffects(positionWS, InstAnimEffectType.PLAYER_LAND_SMOKE);
						}
						else
						{
							host.CreateEquipmentNoRender(positionWS, pos, proto, turn: false);
						}
					});
				}
				else
				{
					host.CreateEquipmentNoRender(positionWS, pos, proto, turn: false);
				}
			})
			.Wait(1f);
	}
}
