using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class TechPointListViewer : DolocHorizontalUI<TechPointRenderer>
{
	[SerializeField]
	private GameObject arrow;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_TECHPOINT_RENDERER);

	public void Render(TechPointSimpleData[] infos)
	{
		if (!infos.IsNullOrEmpty())
		{
			SetCapacity(infos.Length);
			for (int i = 0; i < base.slotCount; i++)
			{
				base.slots[i].Render(infos[i]);
			}
		}
	}

	public override void GetFocus()
	{
		base.GetFocus();
		SelectFirst();
	}

	public override void LoseFocus()
	{
		base.LoseFocus();
		arrow.SetActive(value: false);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		arrow.SetActive(value: false);
	}

	protected override void OnSlotSelect(TechPointRenderer slot)
	{
		base.OnSlotSelect(slot);
		base.isFocused = true;
		arrow.SetActive(value: true);
		arrow.transform.DOLocalMoveX(slot.positionLocal.x + slot.size.x / 2f, 0f);
	}
}
