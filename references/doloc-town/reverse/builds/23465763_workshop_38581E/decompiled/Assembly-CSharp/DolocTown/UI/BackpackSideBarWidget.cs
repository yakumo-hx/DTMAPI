using System;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class BackpackSideBarWidget : InventoryPanel
{
	private MoneyTipSmall moneyTip;

	public int maxLineCapacity;

	protected override void __Init()
	{
		base.__Init();
		ObjectPool<ItemNavSlot> objectPool = slotPool;
		objectPool.OnCreate = (Action<ItemNavSlot>)Delegate.Combine(objectPool.OnCreate, (Action<ItemNavSlot>)delegate(ItemNavSlot s)
		{
			s.backgroundColor = DolocUiColor.BACKCOLOR_LEVEL3;
			s.normalBgColor = DolocUiColor.BACKCOLOR_LEVEL3;
		});
		base.displayAnimType = UiPanelDisplayAnimType.FromRight;
		moneyTip = GetComponentInChildren<MoneyTipSmall>();
		moneyTip.Init();
		SetMode(BackpackSideBarMode.Default);
	}

	protected override int GetLineCapacity(int total)
	{
		int num = 5;
		if (maxLineCapacity > 0)
		{
			return Mathf.Min(num, maxLineCapacity);
		}
		return num;
	}

	public void RefreshMoney(bool useAnimation = true)
	{
		moneyTip.SetMoney(DolocAPI.archiveHandle.CurrentMoney, useAnimation);
	}

	protected override void OnFinishHide()
	{
		SetMode(BackpackSideBarMode.Default);
		base.OnFinishHide();
	}

	public void SetMode(BackpackSideBarMode mode)
	{
		switch (mode)
		{
		case BackpackSideBarMode.Default:
			moneyTip.gameObject.SetActive(value: false);
			break;
		case BackpackSideBarMode.Store:
			moneyTip.gameObject.SetActive(value: true);
			break;
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		SetTitle(base.staticTexts.InventoryPanelBackpackTitle);
	}
}
