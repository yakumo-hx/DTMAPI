using DolocTown.Config;
using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.GameDataTracker;
using DolocTown.UI;

namespace DolocTown;

public class BackpackUpgradeUiState : DolocUiState<BackpackUpPanel>
{
	private BackpackLevelInfo currentLevelProto;

	private BackpackLevelInfo nextLevelProto;

	private int currentCapacity => currentLevelProto.Capacity;

	private int nextCapacity => nextLevelProto.Capacity;

	private int upgradeCost => nextLevelProto.UpgradeCost;

	public bool HandleStartUpArgs()
	{
		int backpackLevel = DolocAPI.archiveHandle.backpackLevel;
		currentLevelProto = DolocConfig.Tables.TbBackpackLevel.GetByLevel(backpackLevel);
		nextLevelProto = DolocConfig.Tables.TbBackpackLevel.GetByLevel(backpackLevel + 1);
		if (currentLevelProto != null)
		{
			return nextLevelProto != null;
		}
		return false;
	}

	private bool BuyCapacity()
	{
		if (DolocAPI.archiveHandle.CurrentMoney < upgradeCost)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackCellExchange);
			return false;
		}
		DolocAPI.archiveHandle.CurrentMoney -= upgradeCost;
		if (DolocAPI.archiveHandle.UpgradeBackpack())
		{
			DataUploader.TraceDayUpgrade($"backpack_{DolocAPI.archiveHandle.farmData.agentData.backpackLevel}", upgradeCost);
		}
		DolocAPI.ShowMessageBoxSmall(string.Format(base.staticTexts.UiBackpackCellExchange, nextCapacity - currentCapacity));
		return true;
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Render(currentCapacity, nextCapacity, upgradeCost, DolocAPI.CanAffordMoney(upgradeCost));
		base.panel.Show();
		base.panel.confirmBtn.Select();
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			OnCancel();
		}
	}

	protected override void Register()
	{
		base.panel.confirmBtn.onClick.AddListener(OnConfirm);
		base.panel.cancelBtn.onClick.AddListener(OnCancel);
	}

	protected override void Unregister()
	{
		base.panel.confirmBtn.onClick.RemoveListener(OnConfirm);
		base.panel.cancelBtn.onClick.RemoveListener(OnCancel);
	}

	private void OnConfirm()
	{
		if (BuyCapacity())
		{
			gameController.PopState();
			DolocAPI.UIRaiseConfirm();
		}
		else
		{
			DolocAPI.UIRaiseCancel();
		}
	}

	private void OnCancel()
	{
		gameController.PopState();
		DolocAPI.UIRaiseCancel();
	}
}
