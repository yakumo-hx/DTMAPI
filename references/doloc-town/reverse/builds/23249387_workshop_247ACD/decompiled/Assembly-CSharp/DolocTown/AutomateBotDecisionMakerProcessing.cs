using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.GameData;
using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateBotDecisionMakerProcessing : AutomateBotDecisionMaker
{
	private readonly AutomateParamProcessing _param;

	private ProcessingMission lastMission;

	public AutomateBotDecisionMakerProcessing(AutomateBot bot, AutomateSystemLocker locker)
		: base(bot, locker)
	{
		_param = (AutomateParamProcessing)bot.Param;
	}

	protected override LinearTask AutomateBotMakeDecision()
	{
		if (TryGetAvailableProcessingMission(out var missionInfo))
		{
			RaiseEmotionIfIdleCount(3, EmotionName.AMAZING);
			return __BuildTaskOfProcessing(missionInfo);
		}
		CountIdle();
		RaiseEmotionIfIdleCount(3, EmotionName.NOCOMMENT, 0.3f);
		return base.FixedTaskWanderAroundStation;
	}

	private LinearTask __BuildTaskOfProcessing(ProcessingMission missionInfo)
	{
		Synthesizer machine = missionInfo.machine;
		return SmartJourney(machine).Do(delegate
		{
			StartProcessing(missionInfo);
		});
	}

	private static void StartProcessing(ProcessingMission missionInfo)
	{
		IRecipe recipe = missionInfo.recipe;
		Synthesizer machine = missionInfo.machine;
		Case[] source = missionInfo.containers.Where((Case x) => !x.IsRemoved).ToArray();
		if (!machine.IsRemoved && !machine.IsWorking)
		{
			int maxCraftCountOfInventories = machine.GetMaxCraftCountOfInventories(recipe, source.Select((Case x) => x.inventory).ToArray());
			if (maxCraftCountOfInventories > 0 && recipe.TryCostInputItemsInInventory(source.Select((Case x) => x.inventory).ToArray(), maxCraftCountOfInventories))
			{
				machine.AutomateCraft(recipe, maxCraftCountOfInventories);
			}
		}
	}

	private bool TryGetAvailableProcessingMission(out ProcessingMission missionInfo)
	{
		HashSet<string> unavailableMachineCache = new HashSet<string>();
		foreach (string recipe2 in _param.Recipes)
		{
			if (DolocAPI.QueryRecipe(recipe2, out var recipe) && TryGetAvailableSynthesizerForRecipe(recipe2, unavailableMachineCache, out var synthesizer))
			{
				Case[] array = base.StationEnv.roomEnvs.SelectMany((AutomateStationEnv.RoomEnv env) => env.GetEquipments<Case>(_param.FilterContainer)).ToArray();
				LinearInventory[] inventories = array.Select((Case x) => x.inventory).ToArray();
				int maxCraftCountOfInventories = synthesizer.GetMaxCraftCountOfInventories(recipe, inventories);
				if (maxCraftCountOfInventories > 0)
				{
					missionInfo = new ProcessingMission(recipe, array, synthesizer, maxCraftCountOfInventories);
					return true;
				}
			}
		}
		missionInfo = default(ProcessingMission);
		return false;
	}

	private Dictionary<string, CountItemContainer[]> CountMaterialsInEnv(CountItem[] items)
	{
		Dictionary<string, List<CountItemContainer>> dictionary = new Dictionary<string, List<CountItemContainer>>();
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			foreach (Case equipment in roomEnvs[i].GetEquipments<Case>(_param.FilterContainer))
			{
				CountItemContainer[] record2 = AutomateProcessingUtils.CountMaterialsInContainer(items, equipment);
				__Record(dictionary, record2);
			}
		}
		return dictionary.ToDictionary((KeyValuePair<string, List<CountItemContainer>> kv) => kv.Value[0].ItemName, (KeyValuePair<string, List<CountItemContainer>> kv) => kv.Value.ToArray());
		static void __Record(Dictionary<string, List<CountItemContainer>> output, CountItemContainer[] record)
		{
			for (int j = 0; j < record.Length; j++)
			{
				CountItemContainer item = record[j];
				if (!output.ContainsKey(item.ItemName))
				{
					output[item.ItemName] = new List<CountItemContainer>();
				}
				output[item.ItemName].Add(item);
			}
		}
	}

	private bool TryGetAvailableSynthesizerForRecipe(string recipeName, HashSet<string> unavailableMachineCache, out Synthesizer synthesizer)
	{
		synthesizer = null;
		if (!DolocConfig.Tables.TbRecipeGroup.TryGetRecipeMachineName(recipeName, out var machineNames))
		{
			return false;
		}
		foreach (string item in machineNames)
		{
			if (!unavailableMachineCache.Contains(item))
			{
				if (TryGetAvailableSynthesizer(item, out synthesizer))
				{
					return true;
				}
				unavailableMachineCache.Add(item);
			}
		}
		return false;
	}

	private bool TryGetAvailableSynthesizer(string equipmentName, out Synthesizer equipment)
	{
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		foreach (AutomateStationEnv.RoomEnv roomEnv in roomEnvs)
		{
			equipment = roomEnv.GetEquipments<Synthesizer>().FirstOrDefault((Synthesizer E) => E.Name == equipmentName && base.locker.IsUnlocked(E) && !E.IsWorking);
			if (equipment != null)
			{
				return true;
			}
		}
		equipment = null;
		return false;
	}
}
