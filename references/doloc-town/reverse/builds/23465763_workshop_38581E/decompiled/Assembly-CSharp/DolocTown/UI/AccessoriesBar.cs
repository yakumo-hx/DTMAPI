using System;
using DG.Tweening;
using DolocTown.GameData;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AccessoriesBar : DolocUIPanel, INavPanel
{
	[SerializeField]
	private DynamicArrow arrow;

	[SerializeField]
	public AccessorySlot hatItem;

	[SerializeField]
	public AccessorySlot positiveItem;

	[SerializeField]
	public AccessorySlot passiveItem1;

	[SerializeField]
	public AccessorySlot passiveItem2;

	private Action hatItemSelect;

	private Action hatItemClick;

	private Action activeItemSelect;

	private Action activeItemClick;

	private Action passiveItem1Select;

	private Action passiveItem1Click;

	private Action passiveItem2Select;

	private Action passiveItem2Click;

	public Selectable[] allSelectablesArray
	{
		get
		{
			if (DolocAPI.archiveHandle.IsAdditionalPassiveSlotUnlocked())
			{
				return new Selectable[4] { hatItem.button, positiveItem.button, passiveItem1.button, passiveItem2.button };
			}
			return new Selectable[3] { hatItem.button, positiveItem.button, passiveItem1.button };
		}
	}

	public int allSelectableCount => allSelectablesArray.Length;

	private AgentEquipmentManager agentEquipment => DolocAPI.archiveHandle.farmData.agentData.agentEquipment;

	protected override void __Init()
	{
		base.__Init();
		arrow.Init();
		hatItem.Init();
		positiveItem.Init();
		passiveItem1.Init();
		passiveItem2.Init();
		hatItem.onSelect.AddListener(delegate
		{
			OnSlotSelect(hatItem);
			hatItemSelect?.Invoke();
		});
		hatItem.SetClickCallbacks(delegate
		{
			hatItemClick?.Invoke();
		}, null, null, null, delegate
		{
			hatItemClick?.Invoke();
		});
		hatItem.onPointerEnter.AddListener(delegate
		{
			hatItem.ShowEquipmentItemViewer(agentEquipment.hatItem, base.staticTexts.EquipmentBarHatTip);
		});
		hatItem.onPointerExit.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
		positiveItem.onSelect.AddListener(delegate
		{
			OnSlotSelect(positiveItem);
			activeItemSelect?.Invoke();
		});
		positiveItem.SetClickCallbacks(delegate
		{
			activeItemClick?.Invoke();
		}, null, null, null, delegate
		{
			activeItemClick?.Invoke();
		});
		positiveItem.onPointerEnter.AddListener(delegate
		{
			positiveItem.ShowEquipmentItemViewer(agentEquipment.activeItem, base.staticTexts.EquipmentBarPositiveTip);
		});
		positiveItem.onPointerExit.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
		passiveItem1.onSelect.AddListener(delegate
		{
			OnSlotSelect(passiveItem1);
			passiveItem1Select?.Invoke();
		});
		passiveItem1.SetClickCallbacks(delegate
		{
			passiveItem1Click?.Invoke();
		}, null, null, null, delegate
		{
			passiveItem1Click?.Invoke();
		});
		passiveItem1.onPointerEnter.AddListener(delegate
		{
			passiveItem1.ShowEquipmentItemViewer(agentEquipment.passiveItem1, base.staticTexts.EquipmentBarPassive1Tip);
		});
		passiveItem1.onPointerExit.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
		passiveItem2.onSelect.AddListener(delegate
		{
			OnSlotSelect(passiveItem2);
			passiveItem2Select?.Invoke();
		});
		passiveItem2.SetClickCallbacks(delegate
		{
			passiveItem2Click?.Invoke();
		}, null, null, null, delegate
		{
			passiveItem2Click?.Invoke();
		});
		passiveItem2.onPointerEnter.AddListener(delegate
		{
			passiveItem2.ShowEquipmentItemViewer(agentEquipment.passiveItem2, base.staticTexts.EquipmentBarPassive2Tip);
		});
		passiveItem2.onPointerExit.AddListener(delegate
		{
			DolocAPI.HideHoverBox();
		});
	}

	public void SetHatItemCallBack(Action select, Action click)
	{
		hatItemSelect = select;
		hatItemClick = click;
	}

	public void SetPositiveItemCallBack(Action select, Action click)
	{
		activeItemSelect = select;
		activeItemClick = click;
	}

	public void SetPassiveItem1CallBack(Action select, Action click)
	{
		passiveItem1Select = select;
		passiveItem1Click = click;
	}

	public void SetPassiveItem2CallBack(Action select, Action click)
	{
		passiveItem2Select = select;
		passiveItem2Click = click;
	}

	public void ClearCallBack()
	{
		hatItemSelect = null;
		hatItemClick = null;
		activeItemSelect = null;
		activeItemClick = null;
		passiveItem1Select = null;
		passiveItem1Click = null;
		passiveItem2Select = null;
		passiveItem2Click = null;
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		arrow.SetVisible(value: false);
	}

	public void SetFocus(AccessorySlot slot)
	{
		slot.highLighted = true;
		ShowArrow(slot);
	}

	public void ResetPanel()
	{
		arrow.SetVisible(value: false);
		hatItem.highLighted = false;
		positiveItem.highLighted = false;
		passiveItem1.highLighted = false;
		passiveItem2.highLighted = false;
		DolocAPI.HideHoverBox();
	}

	private void OnSlotSelect(AccessorySlot slot)
	{
		if (base.isRender)
		{
			slot.GetItemBorder();
			DolocAPI.UIRaiseRoll();
			DolocAPI.uiSystem.inventoryMouse.HoverTo(slot.rectTransform);
		}
	}

	private void ShowArrow(AccessorySlot slot)
	{
		arrow.SetVisible(value: true);
		arrow.transform.DOLocalMoveX(slot.positionLocal.x, 0f);
	}
}
