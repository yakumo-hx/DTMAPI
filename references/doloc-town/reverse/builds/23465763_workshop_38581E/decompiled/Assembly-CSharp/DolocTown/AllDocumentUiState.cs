using System.Collections.Generic;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class AllDocumentUiState : PageUiStateBase<DocumentPanel, DocumentData>
{
	private DocumentData[] unlockedData;

	private ChipDocumentManager chipDocMgr => DolocAPI.archiveHandle.cityData.documentManager.chipDocMgr;

	private PlantDocumentManager plantDocMgr => DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr;

	protected override int totalCapacity
	{
		get
		{
			DocumentData[] array = unlockedData;
			if (array == null)
			{
				return 0;
			}
			return array.Length;
		}
	}

	private IScrollContentRect _contentRect => base.panel.contentRect;

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		List<DocumentData> list = new List<DocumentData>();
		foreach (string item in plantDocMgr.unlockedOrder)
		{
			if (plantDocMgr.unlockedDocuments.TryGetValue(item, out var value))
			{
				list.Add(new DocumentData(value.Title, value.Author, value.Content));
			}
		}
		foreach (string item2 in chipDocMgr.unlockedOrder)
		{
			if (chipDocMgr.unlockedDocuments.TryGetValue(item2, out var value2))
			{
				list.Add(new DocumentData(value2.Title, value2.Author, value2.Content));
			}
		}
		unlockedData = list.ToArray();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		base.OnUiUpdate(deltaTime);
		if (userInput.BaseScrollDir.magnitude > 0f)
		{
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
	}

	protected override DocumentData[] DataGetter(int start, int end)
	{
		List<DocumentData> list = new List<DocumentData>();
		for (int i = start; i < Mathf.Min(end, totalCapacity); i++)
		{
			list.Add(unlockedData[i]);
		}
		return list.ToArray();
	}

	protected override void Show()
	{
		base.Show();
		base.panel.title = base.staticTexts.DocumentTitleComputer;
		base.panel.SetEmptyInfo(base.staticTexts.UiTipEmptyList);
		base.panel.Viewer.SetEmptyInfo(base.staticTexts.DocumentEmpty);
		base.panel.description = string.Format(base.staticTexts.ChipDocumentTotle, totalCapacity);
	}
}
