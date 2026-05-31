using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class FavorabilityHeartViewer : DolocUiRecyclableObject
{
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
		slotPool = new ObjectPool<SingleImage>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_FAVORABILITY_HEART), base.transform, usePreset: true)
		{
			OnRecycle = delegate(SingleImage image)
			{
				image.Sprite = empty;
			}
		};
		slotPool.RecycleAll();
	}

	public void Render(int limit, int likingLevel)
	{
		slotPool.RecycleAll();
		slotPool.CheckCount(Mathf.CeilToInt((float)limit / 2f));
		slotPool.Sort();
		int num = likingLevel / 2 - 1;
		for (int i = 0; i < slotPool.ActiveCount; i++)
		{
			slotPool[i].Sprite = ((i <= num) ? full : empty);
		}
		if (likingLevel % 2 > 0)
		{
			slotPool[num + 1].Sprite = half;
		}
	}
}
