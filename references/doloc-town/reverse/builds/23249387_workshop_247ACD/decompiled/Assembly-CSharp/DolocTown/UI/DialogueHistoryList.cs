using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DialogueHistoryList : DolocUIPanel, IScrollContentRect
{
	[SerializeField]
	private ScrollRect _scrollRect;

	[SerializeField]
	private RectTransform slotRoot;

	[SerializeField]
	private GameObject splitLinePrefab;

	private DialogueLineHistoryData[] dataSource;

	private ObjectPool<DialogueHistoryTextSlot> slotPool;

	private List<GameObject> splitLines = new List<GameObject>();

	protected GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_DIALOGUE_HISTORY_TEXT_SLOT);

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta => 0.05f;

	protected override void __Init()
	{
		base.__Init();
		slotPool = new ObjectPool<DialogueHistoryTextSlot>(slotPrefab, slotRoot, usePreset: true);
		splitLinePrefab.gameObject.SetActive(value: false);
	}

	public void RenderSlots(DialogueBlockHistoryData[] data)
	{
		foreach (GameObject splitLine in splitLines)
		{
			splitLine.gameObject.SetActive(value: false);
		}
		if (data.IsNullOrEmpty())
		{
			slotPool.RecycleAll();
			return;
		}
		int num = data.Length;
		int count = data.Sum((DialogueBlockHistoryData x) => x.dataCount);
		slotPool.CheckCount(count);
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < data.Length; i++)
		{
			foreach (DialogueLineHistoryData data2 in data[i].dataList)
			{
				DialogueHistoryTextSlot dialogueHistoryTextSlot = slotPool[num2++];
				dialogueHistoryTextSlot.Render(data2);
				dialogueHistoryTextSlot.transform.SetSiblingIndex(num4++);
			}
			if (num3 < num - 1)
			{
				while (num3 >= splitLines.Count)
				{
					splitLines.Add(Object.Instantiate(splitLinePrefab, slotRoot.transform));
				}
				GameObject obj = splitLines[num3++];
				obj.transform.SetSiblingIndex(num4++);
				obj.SetActive(value: true);
			}
		}
	}

	public void OnMove()
	{
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		scrollRect.verticalScrollbar.value = 0f;
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		scrollRect.verticalScrollbar.value = 0f;
	}
}
