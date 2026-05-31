using System.Collections.Generic;
using DolocTown.Config.Archives;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class ChipDocumentUiState : PageUiStateBase<DocumentPanel, DocumentData>
{
	private ChipDocumentManager chipDocMgr => DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr;

	protected override int totalCapacity => chipDocMgr.unlockedDocuments.Count;

	protected override DocumentData[] DataGetter(int start, int end)
	{
		List<DocumentData> list = new List<DocumentData>();
		int count = chipDocMgr.unlockedOrder.Count;
		for (int i = start; i < Mathf.Min(end, count); i++)
		{
			string key = chipDocMgr.unlockedOrder[i];
			ChipDocumentInfo chipDocumentInfo = chipDocMgr.unlockedDocuments[key];
			list.Add(new DocumentData(chipDocumentInfo.Title, chipDocumentInfo.Author, chipDocumentInfo.Content));
		}
		return list.ToArray();
	}

	protected override void Show()
	{
		base.Show();
		base.panel.title = base.staticTexts.ChipDocumentTitle;
		base.panel.SetEmptyInfo(base.staticTexts.UiTipEmptyList);
		base.panel.Viewer.SetEmptyInfo(base.staticTexts.DocumentEmpty);
		base.panel.description = string.Format(base.staticTexts.ChipDocumentTotle, chipDocMgr.unlockedDocuments.Count);
	}
}
