using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class CostItemViewer : DolocUiEntity
{
	[SerializeField]
	private Transform slotRoot;

	[SerializeField]
	private int fixedSlotCount;

	[SerializeField]
	private bool alignRight;

	[SerializeField]
	private Color backgroundColor = DolocUiColor.BACKCOLOR_LEVEL1_046ALPHA;

	[SerializeField]
	private bool useItemHoverBox;

	private int _firstIndex;

	private ObjectPool<CostItemSlot> slotPool;

	protected override void __Init()
	{
		base.__Init();
		slotPool = new ObjectPool<CostItemSlot>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_SLOT_COSTITEMICON), slotRoot, usePreset: true);
		slotPool.RecycleAll();
	}

	public void Render(CostViewerData data)
	{
		if (!data.notEmpty)
		{
			SetVisible(value: false);
			return;
		}
		SetVisible(value: true);
		CheckVisibleCount(data.itemNames.Length);
		for (int i = 0; i < data.itemNames.Length; i++)
		{
			CostItemSlot costItemSlot = slotPool[i + _firstIndex];
			costItemSlot.Render(data.itemNames[i], data.costIcons[i], data.costCountInfos[i]);
			costItemSlot.backgroundColor = backgroundColor;
		}
	}

	public void Render(string[] itemNames, Sprite[] icons)
	{
		CheckVisibleCount(itemNames.Length);
		for (int i = 0; i < itemNames.Length; i++)
		{
			CostItemSlot costItemSlot = slotPool[i + _firstIndex];
			costItemSlot.Render(itemNames[i], icons[i], string.Empty);
			costItemSlot.backgroundColor = backgroundColor;
		}
	}

	public void Render(string[] itemNames, Sprite[] icons, bool[] obtain)
	{
		CheckVisibleCount(itemNames.Length);
		for (int i = 0; i < itemNames.Length; i++)
		{
			CostItemSlot costItemSlot = slotPool[i + _firstIndex];
			costItemSlot.Render(itemNames[i], icons[i], string.Empty);
			costItemSlot.backgroundColor = backgroundColor;
			costItemSlot.iconColor = (obtain[i] ? Color.white : Color.black);
			costItemSlot.UseItemHoverBox &= obtain[i];
		}
	}

	public void RaiseCostItemsFadeUp()
	{
		foreach (CostItemSlot item in slotPool)
		{
			if (item.Visible)
			{
				float x = item.position.x - item.rectTransform.sizeDelta.x / 2f;
				float y = item.position.y;
				DolocAPI.RaiseUiSpriteFadeUp(new Vector2(x, y), item.iconSprite);
			}
		}
	}

	public CostItemSlot GetSlot(int index)
	{
		return slotPool[Mathf.Clamp(index, 0, slotPool.ActiveCount - 1)];
	}

	private void CheckVisibleCount(int count)
	{
		int num = Mathf.Max(fixedSlotCount, count);
		slotPool.CheckCount(num);
		_firstIndex = (alignRight ? (num - count) : 0);
		for (int i = 0; i < num; i++)
		{
			bool flag = _firstIndex <= i && i < _firstIndex + count;
			slotPool[i].transform.SetSiblingIndex(i);
			slotPool[i].Visible = flag;
			slotPool[i].UseItemHoverBox = flag && useItemHoverBox;
		}
	}
}
