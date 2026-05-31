using System.Linq;
using DolocTown.Config.Mission;
using RedSaw;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class FactionListViewer : DolocGridUI<CandidateFactionViewer>
{
	[SerializeField]
	private Color normalBgColor;

	[SerializeField]
	private Color selectedBgColor;

	private ObjectPool<TreatyPortFactionViewer> factionPool;

	private UnityAction<int> factionSlotCallback;

	private GameObject factionCard => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_FACTION_SLOT);

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_RECRUIT_SLOT);

	protected override int lineCapacity => 2;

	protected override void __Init()
	{
		base.__Init();
		factionPool = new ObjectPool<TreatyPortFactionViewer>(factionCard, base.transform, usePreset: true);
		factionPool.RecycleAll();
	}

	public void Render(TreatyPortFactionData[] factionDatas)
	{
		slotPool.RecycleAll();
		factionPool.RecycleAll();
		factionPool.CheckCount(factionDatas.Length);
		base.totalCapacity = 4 - factionDatas.Length;
		CheckCount(base.totalCapacity);
		SetSelectCallbacks(null);
		SetClickCallbacks(clickCallback);
		TreatyPortFactionViewer treatyPortFactionViewer = null;
		for (int i = 0; i < factionPool.ActiveCount; i++)
		{
			if (factionDatas[i].factionType == FactionType.KonTiki)
			{
				treatyPortFactionViewer = factionPool[i];
			}
			factionPool[i].Render(factionDatas[i]);
			factionPool[i].transform.SetSiblingIndex(i);
		}
		if (treatyPortFactionViewer != null)
		{
			treatyPortFactionViewer.gameObject.transform.SetSiblingIndex(1);
		}
		foreach (CandidateFactionViewer slot in base.slots)
		{
			slot.ResetPanel();
			slot.backgroundColor = normalBgColor;
			slot.SetClickCallbacks(factionSlotCallback);
		}
		DolocAPI.DelayFrame(BuildNavigation);
	}

	public void BindFactionSlotClickEvent(UnityAction<int> callBack)
	{
		factionSlotCallback = callBack;
	}

	protected override void OnSelectedIndexChange(int oldValue, int newValue)
	{
		CandidateFactionViewer slot = GetSlot(oldValue);
		slot.backgroundColor = normalBgColor;
		slot.ResetPanel();
	}

	protected override void OnSlotSelect(CandidateFactionViewer slot)
	{
		slot.backgroundColor = selectedBgColor;
	}

	public override void BuildNavigation()
	{
		if (slotPool.ActiveCount > 0)
		{
			Selectable[] array = slotPool.Select((CandidateFactionViewer select) => select.button).ToArray();
			Selectable[] array2 = array;
			array2.RebuildNavigationHorizontal(array2, 1f, 90f, wrapAround: false);
			array2.RebuildNavigationVertical(array2, 1f, 90f, wrapAround: false);
		}
	}
}
