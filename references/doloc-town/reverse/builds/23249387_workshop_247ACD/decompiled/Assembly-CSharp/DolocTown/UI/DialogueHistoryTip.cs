using System;

namespace DolocTown.UI;

public class DialogueHistoryTip : DolocUIPanel
{
	public void TryShow(bool useTween = true, Action call = null)
	{
		if (!base.isRender && DolocAPI.archiveHandle.cityData.dialogueManager.historyManager.notEmpty)
		{
			operationTip.SetTextKey(base.staticTexts.UiTipHistory);
			Show(useTween, call);
		}
	}
}
