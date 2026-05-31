using System.Collections.Generic;
using DolocTown.Config;

namespace DolocTown.UI;

public struct EnvOptimizerConsoleBlockData : IUIData
{
	public bool notEmpty { get; }

	public string content { get; }

	public EnvOptimizerConsoleBlockData(EnvOptimizerComponentSlot slot)
	{
		this = default(EnvOptimizerConsoleBlockData);
		if (slot?.CurrentItem == null || slot.SlotInfo == null || !slot.IsActive)
		{
			return;
		}
		notEmpty = true;
		string[] array = slot.SlotInfo.Description.Split('\n');
		List<string> list = new List<string>
		{
			"---",
			DolocUtils.Format(DolocConfig.StaticTexts.UiEnvOptimizerDateInfo, slot.ActiveDateInfo.GetDateInfo()),
			DolocUtils.Format(DolocConfig.StaticTexts.UiEnvOptimizerCheckSuccess, slot.CurrentItem.title.Colored(DolocUiColor.EYECATCHCOLOR_CYAN))
		};
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!text.IsNullOrEmpty())
			{
				list.Add("> " + text);
			}
		}
		list.Add("===\n");
		content = string.Join("\n", list);
	}
}
