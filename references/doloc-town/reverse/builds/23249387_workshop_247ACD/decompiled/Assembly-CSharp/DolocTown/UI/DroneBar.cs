using System.Collections.Generic;
using DG.Tweening;
using DolocTown.Config;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DroneBar : DolocHorizontalUI<DroneItemSimpleSlot>, INavPanel
{
	[SerializeField]
	public DroneItemSimpleSlot structItemSlot;

	[SerializeField]
	private CanvasGroup content;

	[SerializeField]
	private Text emptyHint;

	[SerializeField]
	private DynamicArrow arrow;

	private const int StructSlotIndex = 5;

	private int highLightIndex = -1;

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_DRONE_SIMPLE_SLOT);

	public Selectable[] allSelectablesArray
	{
		get
		{
			List<Selectable> list = new List<Selectable> { structItemSlot.button };
			foreach (DroneItemSimpleSlot slot in base.slots)
			{
				list.Add(slot.button);
			}
			return list.ToArray();
		}
	}

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		arrow.Init();
		structItemSlot.Init();
		structItemSlot.index = 5;
		structItemSlot.onSelect.AddListener(delegate(int index)
		{
			base.selectedIndex = index;
			OnSlotSelect(structItemSlot);
		});
		structItemSlot.onDeselect.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
		structItemSlot.onPointerExit.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
	}

	public void Render(DroneBarData data)
	{
		content.alpha = (data.notEmpty ? 1 : 0);
		emptyHint.text = DolocConfig.StaticTexts.EquipmentBarDroneNotEquip;
		emptyHint.gameObject.SetActive(!data.notEmpty);
		structItemSlot.Render(data.structure);
		SetCapacity(data.comCount);
		for (int i = 0; i < data.comCount; i++)
		{
			GetSlot(i).Render(data.comSprites[i], data.comTypes[i]);
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		arrow.SetVisible(value: false);
	}

	public override void BuildNavigation()
	{
	}

	public void SetStructureFocus()
	{
		SetFocus(5);
	}

	public void SetFocus(int index)
	{
		highLightIndex = index;
		GetSlot(index).highLighted = true;
		ShowArrow(index);
	}

	protected override void OnSlotSelect(DroneItemSimpleSlot slot)
	{
		if (base.isRender)
		{
			slot.GetItemBorder();
			DolocAPI.UIRaiseRoll();
			DolocAPI.uiSystem.inventoryMouse.HoverTo(slot.rectTransform);
		}
	}

	protected override void OnSlotDeselect(DroneItemSimpleSlot slot)
	{
		DolocAPI.HideHoverBox();
	}

	protected override void OnSlotPointerExit(DroneItemSimpleSlot slot)
	{
		DolocAPI.HideHoverBox();
	}

	public void ResetPanel()
	{
		if (highLightIndex < 0)
		{
			return;
		}
		base.selectedIndex = -1;
		structItemSlot.highLighted = false;
		foreach (DroneItemSimpleSlot slot in base.slots)
		{
			slot.highLighted = false;
		}
		arrow.SetVisible(value: false);
		DolocAPI.HideHoverBox();
	}

	public override DroneItemSimpleSlot GetSlot(int index)
	{
		if (index == 5)
		{
			return structItemSlot;
		}
		return base.GetSlot(index);
	}

	private void ShowArrow(int index)
	{
		arrow.SetVisible(value: true);
		arrow.transform.DOMoveX(GetSlot(index).position.x, 0f);
	}
}
