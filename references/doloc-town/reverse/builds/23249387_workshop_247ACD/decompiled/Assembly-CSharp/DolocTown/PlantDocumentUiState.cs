using System.Collections.Generic;
using DolocTown.Config.Archives;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class PlantDocumentUiState : PageUiStateBase<DocumentPanel, DocumentData>
{
	private PlantDocumentManager plantDocMgr => DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr;

	protected override int totalCapacity => plantDocMgr.unlockedDocuments.Count;

	protected override DocumentData[] DataGetter(int start, int end)
	{
		List<DocumentData> list = new List<DocumentData>();
		int count = plantDocMgr.unlockedOrder.Count;
		for (int i = start; i < Mathf.Min(end, count); i++)
		{
			string key = plantDocMgr.unlockedOrder[i];
			PlantDocumentInfo plantDocumentInfo = plantDocMgr.unlockedDocuments[key];
			list.Add(new DocumentData(plantDocumentInfo.Title, plantDocumentInfo.Author, plantDocumentInfo.Content));
		}
		return list.ToArray();
	}

	protected override void Show()
	{
		base.Show();
		base.panel.title = base.staticTexts.PlantDocumentTitle;
		base.panel.SetEmptyInfo(base.staticTexts.UiTipEmptyList);
		base.panel.Viewer.SetEmptyInfo(base.staticTexts.DocumentEmpty);
		base.panel.description = string.Format(base.staticTexts.ChipDocumentTotle, plantDocMgr.unlockedDocuments.Count);
	}
}
