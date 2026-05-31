using UnityEngine.Events;

namespace DolocTown;

public class EnvOptimizerSubmitUiState : ContainerBaseUiState
{
	private EnvOptimizerContainer envContainer;

	private int firstSelectedIndex;

	protected override bool singleOption => true;

	protected override bool disablePutMax => true;

	protected override bool disablePutAll => true;

	protected override bool disableUpdateTip => true;

	protected override bool disableDestroy => true;

	protected override UnityEvent OnCloseButtonClick => base.backpackPanel.OnCloseButtonClick;

	public bool HandleStartUpArgs(IContainer container, int firstSelectedIndex)
	{
		if (!base.HandleStartUpArgs(container))
		{
			return false;
		}
		if (!(container is EnvOptimizerContainer envOptimizerContainer))
		{
			return false;
		}
		envContainer = envOptimizerContainer;
		this.firstSelectedIndex = firstSelectedIndex;
		return true;
	}

	protected override void OnBackpackItemClick(int index)
	{
		if (!base.container.ContentFilter(base.selectedItem))
		{
			if (base.selectedItem != null)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
			}
		}
		else
		{
			Item item = base.containerInventory.Take(0);
			Item item2 = base.backpackInventory.Take(index);
			base.containerInventory.PlaceItem(item2);
			base.backpackInventory.PlaceItemAt(index, item);
		}
	}

	protected override void OnContainerItemClick(int index)
	{
		PlaceToOtherSide();
	}

	protected override void Register()
	{
		base.Register();
		envContainer.BindEnvOptimizerSystem();
	}

	protected override void Unregister()
	{
		base.Unregister();
		envContainer.UnbindEnvOptimizerSystem();
	}

	protected override bool HandleInput(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed || userInput.BaseSubmitItem)
		{
			gameController.PopState();
			return true;
		}
		return false;
	}

	protected override void Sort()
	{
		base.backpackInventory.Sort();
	}

	protected override void Show()
	{
		base.Show();
		base.backpackPanel.SetCloseButtonVisible(value: true);
		base.backpackPanel.SetDeleteCallback(null);
		base.containerWidget.SetCloseButtonVisible(value: false);
		base.panel.SetRaycastMaskEnable(value: true);
		DolocAPI.HideItemBorder();
		DolocAPI.DelayFrame(delegate
		{
			DolocAPI.HideItemBorder();
			base.containerWidget.Select(firstSelectedIndex);
		});
	}

	protected override string[] GetTipInBackpack()
	{
		return new string[2]
		{
			base.staticTexts.UiTipTakeOutAll,
			base.staticTexts.UiTipDestroyItem
		};
	}
}
