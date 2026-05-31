using System;
using System.Linq;
using DolocTown.UI;

namespace DolocTown;

public class ParkingApronUiState : ContainerBaseUiState
{
	private ParkingApron parkingApron;

	private bool couldLaunch => parkingApron.CouldLaunch;

	protected override bool ignoreSubmitWhenClick => true;

	public override bool HandleStartUpArgs(IContainer container, Action onExit = null)
	{
		if (!(container is ParkingApron))
		{
			return false;
		}
		parkingApron = (ParkingApron)container;
		return base.HandleStartUpArgs(container, onExit);
	}

	protected override string[] GetTipInBackpack()
	{
		return new string[4]
		{
			base.staticTexts.UiTipLaunchDrone,
			base.staticTexts.UiTipPutMax,
			base.staticTexts.UiTipPutAll,
			base.staticTexts.UiTipTidy
		}.Concat(GetBackpackTipsByInput()).ToArray();
	}

	protected override string[] GetTipInContainer()
	{
		return new string[4]
		{
			base.staticTexts.UiTipLaunchDrone,
			base.staticTexts.UiTipTakeOutMax,
			base.staticTexts.UiTipTakeOutAll,
			base.staticTexts.UiTipTidy
		}.Concat(GetContainerTipsByInput()).ToArray();
	}

	protected override void Show()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEventShow);
		base.containerWidget.SetLaunchCallback(OnLaunchButtonClick);
		base.containerWidget.DisableLaunchButton(!parkingApron.CouldLaunch);
		RefreshInfo();
		base.Show();
		base.backpackPanel.SetDeleteCallback(null);
	}

	private void OnLaunchButtonClick()
	{
		currentPanel.Select(base.currentIndex);
		if (!couldLaunch)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.DropoffBoxNoDrone);
			return;
		}
		if (base.containerInventory.isEmpty)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.DropoffBoxNoGoods);
			return;
		}
		DolocAPI.ShowQuestionBox(string.Format(base.staticTexts.DropoffBoxConfirmLauch, parkingApron.InventoryTotalPrice), delegate
		{
			gameController.PopState();
			DolocAPI.Delay(0.3f, parkingApron.Launch);
		});
	}

	private void RefreshInfo()
	{
		base.containerWidget.SetInfo(parkingApron.GetCurrentContainerInfo());
	}

	private void RefreshInfo(int index, Item item, bool _)
	{
		RefreshInfo();
	}

	protected override void Register()
	{
		base.Register();
		base.containerInventory.AddReceiver(RefreshInfo);
	}

	protected override void Unregister()
	{
		base.Unregister();
		base.containerInventory.RemoveReceiver(RefreshInfo);
	}

	protected override ItemData ConvertItemData(Item item)
	{
		float priceScale;
		int itemUnitPrice = parkingApron.GetItemUnitPrice(item, out priceScale);
		return ItemData.ShowWithPrice(item, itemUnitPrice, priceScale);
	}

	protected override void DestroyItem()
	{
	}

	protected override void DisposeItem()
	{
	}

	protected override bool HandleInput(float deltaTime)
	{
		if (base.HandleInput(deltaTime))
		{
			return true;
		}
		if (userInput.BaseSubmitItem)
		{
			OnLaunchButtonClick();
			return true;
		}
		return false;
	}
}
