using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class PrestigeTrophyViewer : DolocUiObject
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Sprite empty;

	[SerializeField]
	private Sprite half;

	[SerializeField]
	private Sprite full;

	private ObjectPool<SingleImage> slotPool;

	protected override void __Init()
	{
		base.__Init();
		slotPool = new ObjectPool<SingleImage>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_TROPHY_SLOT), base.transform, usePreset: true);
		slotPool.RecycleAll();
	}

	public void Render(int maxLv, int lv)
	{
		title.text = base.staticTexts.TreatyPortFactionReputation;
		slotPool.CheckCount(maxLv / 2);
		lv = Mathf.Min(lv, maxLv);
		int num = lv / 2 - 1;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			slotPool[i].Sprite = ((i <= num) ? full : empty);
		}
		if (lv % 2 > 0)
		{
			slotPool[num + 1].Sprite = half;
		}
	}
}
