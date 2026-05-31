using System.Linq;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown.UI;

public struct EnvOptimizerData : IUIData
{
	public bool notEmpty { get; }

	public string title { get; }

	public EnvOptimizerBranchData[] branches { get; }

	public EnvOptimizerItemData[] items { get; }

	public string consoleText { get; }

	public string overviewText { get; }

	public int[] stepScores { get; }

	public int availableStep { get; }

	public int overviewScore { get; }

	public string buttonText { get; }

	public bool canActive { get; }

	public EnvOptimizerData(EnvOptimizerSystem data)
	{
		this = default(EnvOptimizerData);
		if (data != null)
		{
			notEmpty = true;
			title = DolocConfig.Tables.TbStaticText.UiEnvOptimizerPanelTitle;
			branches = data.data.branches.Values.Select((EnvOptimizerBranch x) => new EnvOptimizerBranchData(x)).ToArray();
			items = data.slots.Select((EnvOptimizerComponentSlot x) => new EnvOptimizerItemData(x)).ToArray();
			string text = $"{data.TotalPower.ToString()}/{data.TotalLimitation}";
			if (Mathf.Approximately(data.TotalProcess, 1f))
			{
				text = text.Colored(DolocUiColor.EYECATCHCOLOR_CYAN);
			}
			overviewText = DolocUtils.Format(DolocConfig.Tables.TbStaticText.UiEnvOptimizerOverview, text);
			overviewScore = data.TotalPower;
			availableStep = data.GetAvailableSlotCount();
			EnvOptimizerConsoleBlockData[] source = (from x in data.slots
				where x.IsActive
				select new EnvOptimizerConsoleBlockData(x)).ToArray();
			consoleText = string.Join("\n", source.Select((EnvOptimizerConsoleBlockData x) => x.content)).Trim();
			stepScores = data.componentRequirePowerLevels.Select((int x) => x).ToArray();
			canActive = data.CanActive(out var isEnergyEnough, out var isComponentEnough);
			buttonText = DolocConfig.StaticTexts.UiEnvOptimizerButtonNotValid;
			if (data.IsAllActive())
			{
				buttonText = "";
			}
			else if (canActive)
			{
				buttonText = DolocConfig.StaticTexts.UiEnvOptimizerButtonValid;
			}
			else if (!isEnergyEnough)
			{
				buttonText = DolocConfig.StaticTexts.UiEnvOptimizerButtonNoEnergy;
			}
			else if (!isComponentEnough)
			{
				buttonText = DolocConfig.StaticTexts.UiEnvOptimizerButtonNoComponent;
			}
		}
	}
}
