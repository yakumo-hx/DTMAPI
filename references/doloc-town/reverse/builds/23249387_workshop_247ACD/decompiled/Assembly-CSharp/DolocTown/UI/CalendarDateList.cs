using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class CalendarDateList : DolocGridUI<DateSlot>
{
	[SerializeField]
	private Transform selectedHint;

	private Sequence _hintMove;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_DATE_SLOT);

	public void RefreshView(DateData[] datas)
	{
		int num = ((datas != null) ? datas.Length : 0);
		CheckCount(num);
		for (int i = 0; i < num; i++)
		{
			DateData data = datas[i];
			DateSlot dateSlot = slotPool[i];
			if (data.notEmpty)
			{
				dateSlot.visible = true;
				dateSlot.Render(data);
			}
			else
			{
				dateSlot.visible = false;
			}
		}
		SetCapacity(num, slotLayoutGroup.constraintCount);
	}

	protected override void OnSlotSelect(DateSlot slot)
	{
		base.OnSlotSelect(slot);
		HoverTo(slot);
	}

	private void HoverTo(DateSlot slot)
	{
		if (!(slot == null))
		{
			selectedHint.SetParent(slot.rectTransform);
			Vector2 localPositionByAnchor = slot.rectTransform.GetLocalPositionByAnchor(UIAlignmentType.LeftTop);
			_hintMove?.Kill();
			_hintMove = DOTween.Sequence();
			_hintMove.Append(selectedHint.DOLocalMove(localPositionByAnchor, 0.2f)).SetEase(Ease.OutExpo);
		}
	}
}
