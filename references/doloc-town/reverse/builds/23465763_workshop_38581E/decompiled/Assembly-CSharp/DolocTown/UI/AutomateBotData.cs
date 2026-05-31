using DolocTown.Config;

namespace DolocTown.UI;

public struct AutomateBotData : IUIData
{
	public string botName { get; }

	public string runningState { get; }

	public bool notEmpty { get; }

	public bool hasCfg { get; }

	public string optionItem1Title { get; }

	public bool optionItem1 { get; }

	public string optionItem2Title { get; }

	public bool optionItem2 { get; }

	public bool hasRecipeList { get; }

	public AutomateBotData(AutomateBot bot)
	{
		this = default(AutomateBotData);
		notEmpty = bot != null;
		if (bot == null)
		{
			return;
		}
		botName = DolocAPI.GetItemTitle(bot.proto.Id);
		if (bot.IsCharging)
		{
			runningState = DolocConfig.StaticTexts.AutomateBotPanelStateCharge;
		}
		else if (bot.IsWaiting)
		{
			runningState = DolocConfig.StaticTexts.AutomateBotPanelStateIdle;
		}
		else if (bot.IsPause)
		{
			runningState = DolocConfig.StaticTexts.AutomateBotPanelStatePause;
		}
		else
		{
			runningState = DolocConfig.StaticTexts.AutomateBotPanelStateWorking;
		}
		AutomateBotDecisionMaker decisionMaker = bot.DecisionMaker;
		if (!(decisionMaker is AutomateBotDecisionMakerFarming))
		{
			if (!(decisionMaker is AutomateBotDecisionMakerFilling))
			{
				if (!(decisionMaker is AutomateBotDecisionMakerGathering))
				{
					if (decisionMaker is AutomateBotDecisionMakerProcessing)
					{
						hasCfg = true;
						optionItem1 = true;
						optionItem2 = true;
						hasRecipeList = true;
						optionItem1Title = DolocConfig.StaticTexts.AutomateBotPanelRecipeType;
						optionItem2Title = DolocConfig.StaticTexts.AutomateBotPanelRecipeSubType;
					}
					else
					{
						hasCfg = false;
					}
				}
				else
				{
					hasCfg = false;
				}
			}
			else
			{
				hasCfg = true;
				optionItem1 = true;
				optionItem1Title = DolocConfig.StaticTexts.AutomateBotPanelEnergyType;
			}
		}
		else
		{
			hasCfg = true;
			optionItem1 = true;
			optionItem2 = true;
			optionItem1Title = DolocConfig.StaticTexts.AutomateBotPanelAutoFertilizer;
			optionItem2Title = DolocConfig.StaticTexts.AutomateBotPanelAutoProtect;
		}
	}
}
