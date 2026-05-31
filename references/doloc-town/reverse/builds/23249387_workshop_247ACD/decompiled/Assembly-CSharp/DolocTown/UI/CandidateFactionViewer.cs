using System.Linq;
using RedSaw;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CandidateFactionViewer : DolocNavigationButton
{
	[SerializeField]
	private GameObject emptyContent;

	[SerializeField]
	private Text emptyHintText;

	[SerializeField]
	private Text title;

	[SerializeField]
	private CanvasGroup content;

	[SerializeField]
	private Transform container;

	private ObjectPool<CandidateFactionSlot> slotPool;

	private UnityAction<int> clickCallback;

	protected override void __Init()
	{
		base.__Init();
		slotPool = new ObjectPool<CandidateFactionSlot>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_FACTION_CARD_SLOT), container, usePreset: true);
		slotPool.RecycleAll();
	}

	public void ShowFactionList(RecruitData data)
	{
		slotPool.CheckCount(data.factionCount);
		slotPool.Sort();
		SetClickCallbacks(clickCallback);
		emptyContent.gameObject.SetActive(!data.notEmpty);
		content.alpha = ((data.factionCount > 0) ? 1 : 0);
		content.blocksRaycasts = true;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			slotPool[i].Render(data.factionDatas[i]);
		}
		DolocAPI.DelayFrame(RebuildNavigation);
	}

	public void ResetPanel()
	{
		emptyContent.gameObject.SetActive(value: true);
		content.alpha = 0f;
		content.blocksRaycasts = false;
		emptyHintText.text = base.staticTexts.TreatyPortRecruitHint;
		title.text = base.staticTexts.TreatyPortBroadcast;
	}

	public void SelectFactionSlot(int index)
	{
		if (index >= 0 && index < slotPool.ActiveCount)
		{
			slotPool[index].Select();
		}
	}

	public void SetClickCallbacks(UnityAction<int> callback)
	{
		clickCallback = callback;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			CandidateFactionSlot candidateFactionSlot = slotPool[i];
			candidateFactionSlot.index = i;
			candidateFactionSlot.onClick.RemoveAllListeners();
			if (callback != null)
			{
				candidateFactionSlot.onClick.AddListener(callback);
			}
		}
	}

	private void RebuildNavigation()
	{
		if (slotPool.ActiveCount > 0)
		{
			Selectable[] array = slotPool.Select((CandidateFactionSlot select) => select.button).ToArray();
			Selectable[] array2 = array;
			array2.RebuildNavigationHorizontal(array2);
		}
	}
}
