using System.Linq;
using DolocTown.GameData;
using RedSaw;
using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateBotDecisionMakerLogistics : AutomateBotDecisionMaker
{
	private readonly AutomateParamLogistics _paramLogistics;

	public AutomateBotDecisionMakerLogistics(AutomateBot bot, AutomateSystemLocker locker)
		: base(bot, locker)
	{
		_paramLogistics = (AutomateParamLogistics)base.Bot.Param;
	}

	protected override LinearTask AutomateBotMakeDecision()
	{
		if (base.BotInventory.isEmpty)
		{
			if (TryGetTransportMission(out var outputContainer))
			{
				return SmartJourney(outputContainer).AutomateTakeItems(outputContainer, base.Bot.LeftInventorySpace, (Item item) => item.disposable);
			}
			return BuildIdleTask();
		}
		if (TryGetInputContainer(out var inputContainer))
		{
			return SmartJourney(inputContainer).ReleaseItemsAsPossible(inputContainer);
		}
		return BuildIdleTask();
	}

	private LinearTask BuildIdleTask()
	{
		if (RandomUtils.Dice(0.5f))
		{
			return base.FixedTaskWanderAroundStation;
		}
		return base.FixedTaskWanderAroundStation.Emotion(EmotionName.NOCOMMENT);
	}

	private bool TryGetInputContainer(out Case inputContainer)
	{
		inputContainer = null;
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			foreach (Case equipment in roomEnvs[i].GetEquipments<Case>())
			{
				if (_paramLogistics.MatchInput(equipment) && IsValidInputContainer(equipment, base.BotInventory))
				{
					inputContainer = equipment;
					return true;
				}
			}
		}
		return false;
		static bool IsValidInputContainer(Case inputContainer, LinearInventory currentInventory)
		{
			return currentInventory.ReadAll().Any((Item x) => inputContainer.inventory.CanPlaceIn(x));
		}
	}

	private bool TryGetTransportMission(out Case outputContainer)
	{
		outputContainer = null;
		bool flag = false;
		bool flag2 = false;
		AutomateStationEnv.RoomEnv[] roomEnvs = base.StationEnv.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			foreach (Case equipment in roomEnvs[i].GetEquipments<Case>())
			{
				if (!flag && _paramLogistics.MatchInput(equipment))
				{
					flag = true;
				}
				if (!flag2 && _paramLogistics.MatchOutput(equipment))
				{
					outputContainer = equipment;
					flag2 = true;
				}
			}
		}
		return flag && flag2;
	}
}
