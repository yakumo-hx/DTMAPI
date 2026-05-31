using System;
using UnityEngine;

namespace DolocTown.UI;

public class EquipmentPanel : CraftGridPanel<EquipmentSlot, EquipmentViewer, EquipmentData>
{
	[SerializeField]
	public EquipmentSubMenuUI menuUI;

	public Action<int> OnCollectBtnClick;

	public EquipmentViewer EquipmentViewer => viewer;

	public DolocNavigationButton BtnCollect => EquipmentViewer.BtnCollect;

	protected override void OnInitSlot(EquipmentSlot slot)
	{
		base.OnInitSlot(slot);
		slot.onSelect.AddListener(delegate
		{
			slot.backgroundColor = slot.selectedColor;
		});
		slot.onDeselect.AddListener(delegate
		{
			slot.backgroundColor = (slot.IsUnLock ? slot.highlightColor : slot.normalColor);
		});
		slot.BtnCollect.onClick.AddListener(delegate
		{
			OnCollectBtnClick(base.offset + slot.index);
		});
	}

	protected override void RenderSlot(EquipmentSlot slot, EquipmentData data)
	{
		base.RenderSlot(slot, data);
		slot.backgroundColor = (data.isUnlock ? slot.highlightColor : slot.normalColor);
		slot.backgroundColor = (slot.IsSelected ? slot.selectedColor : slot.backgroundColor);
	}
}
