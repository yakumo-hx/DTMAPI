using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.GameData;
using RedSaw;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public static class LinearMissionPatch
{
	public static LinearTask __AutomateBotJourneyIntoBuildingSameScene(this LinearTask preTask, Vector2 botPosition, TemplateRoomInHouse buildingRoom, int moveSpeed)
	{
		if (buildingRoom.Building.IsNotCellar())
		{
			return preTask.__AutomateBotJourneyIntoBuildingSameScene(buildingRoom.Building, moveSpeed);
		}
		Building building = (from b in buildingRoom.Buildings.ToArray()
			orderby Vector2.Distance(botPosition, b.EntryPosition)
			select b).FirstOrDefault();
		if (building == null)
		{
			return preTask.__AutomateBotJourneyIntoBuildingSameScene(buildingRoom.Building, moveSpeed);
		}
		return preTask.__AutomateBotJourneyIntoBuildingSameScene(building, moveSpeed);
	}

	private static LinearTask __AutomateBotJourneyIntoBuildingSameScene(this LinearTask preTask, Building building, int moveSpeed)
	{
		return preTask.Then(new AutomateTaskMove(building.EntryPosition + new Vector2(0f, 3f))).Then(new AutomateTaskEnterBuilding(building));
	}

	public static LinearTask AutomateDo(this LinearTask mission, Action<AutomateBot> action)
	{
		return mission.Then(new AutomateTaskBotAction(action));
	}

	public static LinearTask AutomateMove(this LinearTask preTask, Vector2 target, Room futureRoom = null, LinearTaskBreaker breaker = null)
	{
		return preTask.Then(new AutomateTaskMove(target, futureRoom, breaker));
	}

	public static LinearTask AutomatePlant(this LinearTask preTask, AutomatePlantMissionInfo missionInfo)
	{
		return preTask.Then(new AutomateTaskPlant(missionInfo));
	}

	public static LinearTask AutomateProtectUseLocalItem(this LinearTask preTask, PlantBasin basin)
	{
		return preTask.Then(new AutomateTaskProtect(basin));
	}

	public static LinearTask AutomateFertilizerUseLocalItem(this LinearTask preTask, PlantBasin basin)
	{
		return preTask.Then(new AutomateTaskFertilizer(basin));
	}

	public static LinearTask AutomateWatering(this LinearTask preTask, PlantBasin[] basins, int cost)
	{
		return preTask.Then(new AutomateTaskWatering(basins, cost));
	}

	public static LinearTask AutomateHarvest(this LinearTask preTask, PlantBasin basin)
	{
		return preTask.Then(new AutomateTaskHarvestCrop(basin));
	}

	public static LinearTask AutomateGatherEquipment(this LinearTask preTask, Equipment equipment)
	{
		return preTask.Then(new AutomateTaskGatherEquipment(equipment));
	}

	public static LinearTask AutomatePickUp(this LinearTask preTask, DropItemBase dropItem)
	{
		return preTask.Then(new AutomateTaskPickupItem(dropItem));
	}

	public static LinearTask AutomateTakeItems(this LinearTask task, Case container, int count, Func<Item, bool> predict)
	{
		return task.AutomateDo(delegate(AutomateBot bot)
		{
			Item[] array = container.inventory.AutomatePatchTakeItems(predict, count);
			Queue<Item> queue = new Queue<Item>();
			List<Sprite> list = new List<Sprite>();
			Item[] array2 = array;
			foreach (Item item in array2)
			{
				Item item2 = bot.inventory.PlaceItem(item);
				if (item2 != null)
				{
					queue.Enqueue(item2);
				}
				else
				{
					list.Add(item.proto.UiSpriteAsset.Asset);
				}
			}
			while (queue.Count > 0)
			{
				container.inventory.PlaceItem(queue.Dequeue());
			}
			bot.RaiseSpriteArrayFadeUp(list.ToArray());
		});
	}

	public static LinearTask AutomateTakeItems<T>(this LinearTask preTask, Case container, int count, Func<T, bool> predict) where T : Item
	{
		return preTask.AutomateDo(delegate(AutomateBot bot)
		{
			T[] array = container.inventory.AutomatePatchTakeItems(predict, count);
			Queue<Item> queue = new Queue<Item>();
			HashSet<Sprite> hashSet = new HashSet<Sprite>();
			T[] array2 = array;
			foreach (T val in array2)
			{
				Item item = bot.inventory.PlaceItem(val);
				if (item != null)
				{
					queue.Enqueue(item);
				}
				else
				{
					hashSet.Add(val.proto.UiSpriteAsset.Asset);
				}
			}
			while (queue.Count > 0)
			{
				container.inventory.PlaceItem(queue.Dequeue());
			}
			bot.RaiseSpriteArrayFadeUp(hashSet.ToArray());
		});
	}

	public static LinearTask ReleaseItemsAsPossible(this LinearTask preTask, Case container)
	{
		return preTask.AutomateDo(delegate(AutomateBot bot)
		{
			Queue<Item> queue = new Queue<Item>();
			HashSet<Sprite> hashSet = new HashSet<Sprite>();
			Item[] array = bot.inventory.ReadAll();
			foreach (Item item in array)
			{
				if (container.IsAutomateLabelMatch(item))
				{
					bot.inventory.Take(item);
					Item item2 = container.inventory.PlaceItem(item);
					if (item2 != null)
					{
						queue.Enqueue(item2);
					}
					else
					{
						hashSet.Add(item.uiSprite);
					}
				}
			}
			Debug.Log("[Automate]尝试放置道具，成功放置了" + (bot.inventory.ReadAll().Length - queue.Count) + "个，道具不足以放置的有" + hashSet.Count + "个");
			while (queue.Count > 0)
			{
				bot.inventory.PlaceItem(queue.Dequeue());
			}
			bot.RaiseSpriteArrayFadeUp(hashSet.ToArray(), fadeUp: false);
		});
	}

	public static LinearTask AutomateAddFuel(this LinearTask preTask, PowerGenerator generator, Func<PowerGenerator, bool> isLowPowerCondition)
	{
		return preTask.AutomateDo(delegate(AutomateBot bot)
		{
			bot.locker.UnlockEquipment(generator);
			Item[] array = bot.inventory.ReadAll();
			foreach (Item item in array)
			{
				if (generator.IsSuitableFuel(item) && item.CostSelfFromInventory(bot.inventory, out var _, showFadeUpIcon: false, isQuickInventory: false))
				{
					bot.RaiseSpriteFadeUp(item.uiSprite);
					generator.AddFuel(item.proto);
					if (!isLowPowerCondition(generator))
					{
						break;
					}
				}
			}
		});
	}

	public static LinearTask AutomateFillAnimalFeeds(this LinearTask task, Feeder feeder, Func<Feeder, bool> isLowFeedsCondition)
	{
		return task.AutomateDo(delegate(AutomateBot bot)
		{
			bot.locker.UnlockEquipment(feeder);
			Item[] array = bot.inventory.ReadAll();
			foreach (Item item in array)
			{
				if (feeder.IsAnimalFeeds(item.proto, out var _) && item.CostSelfFromInventory(bot.inventory, out var _, showFadeUpIcon: false, isQuickInventory: false))
				{
					bot.RaiseSpriteFadeUp(item.uiSprite);
					feeder.AddFeeds(item.proto);
					if (!isLowFeedsCondition(feeder))
					{
						break;
					}
				}
			}
		});
	}

	public static LinearTask AutomateFillFishFeeds(this LinearTask task, IFishTank fishTank, Func<IFishTank, bool> isLowFeedsCondition)
	{
		return task.AutomateDo(delegate(AutomateBot bot)
		{
			Item[] array = bot.inventory.ReadAll();
			foreach (Item item in array)
			{
				if (DolocConfig.Tables.TbFishFeed.IsFishFeeds(item.name, out var _) && item.CostSelfFromInventory(bot.inventory, out var _, showFadeUpIcon: false, isQuickInventory: false))
				{
					bot.RaiseSpriteFadeUp(item.uiSprite);
					fishTank.AddFeeds(item.proto);
					if (!isLowFeedsCondition(fishTank))
					{
						break;
					}
				}
			}
		});
	}

	public static LinearTask Emotion(this LinearTask mission, EmotionName emotion)
	{
		return mission.AutomateDo(delegate(AutomateBot bot)
		{
			bot.RaiseEmotion(emotion);
		});
	}

	public static LinearTask Emotion(this LinearTask mission, EmotionName emotion, float dice)
	{
		return mission.AutomateDo(delegate(AutomateBot bot)
		{
			if (RandomUtils.Dice(dice))
			{
				bot.RaiseEmotion(emotion);
			}
		});
	}
}
