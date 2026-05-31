using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class EnvOptimizerUiState : DolocUiState<EnvOptimizerPanel>
{
	private int selectedIndex;

	private bool ignoreInput;

	public static string latestActivatedComponent { get; private set; } = "";


	private EnvOptimizerSystem envOptimizerSystem => DolocAPI.archiveHandle.farmData.envOptimizerSystem;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private IScrollContentRect _contentRect => base.panel.contentRect;

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		ignoreInput = false;
	}

	protected override void Register()
	{
		EnvOptimizerItemSlot[] itemSlots = base.panel.itemSlots;
		foreach (EnvOptimizerItemSlot obj in itemSlots)
		{
			obj.onClick.AddListener(OnItemSlotClick);
			obj.onSelect.AddListener(OnItemSlotSelect);
		}
		base.panel.btnConfirm.onClick.AddListener(OnBtnConfirmClick);
	}

	protected override void Unregister()
	{
		EnvOptimizerItemSlot[] itemSlots = base.panel.itemSlots;
		foreach (EnvOptimizerItemSlot obj in itemSlots)
		{
			obj.onClick.RemoveListener(OnItemSlotClick);
			obj.onSelect.RemoveListener(OnItemSlotSelect);
		}
		base.panel.btnConfirm.onClick.RemoveListener(OnBtnConfirmClick);
	}

	private void OnItemSlotClick(int idx)
	{
		if (!envOptimizerSystem.CheckSlotIndexValid(idx))
		{
			return;
		}
		EnvOptimizerComponentSlot envOptimizerComponentSlot = envOptimizerSystem.slots[idx];
		if (envOptimizerComponentSlot.IsActive)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.UiEnvOptimizerComponentAlreadyActive, envOptimizerComponentSlot.CurrentItem.title));
			return;
		}
		EnvOptimizerContainer container = new EnvOptimizerContainer(idx);
		if (container.Valid)
		{
			DolocAPI.EnterUI((EnvOptimizerSubmitUiState state) => state.HandleStartUpArgs(container, selectedIndex));
		}
	}

	private void OnItemSlotSelect(int idx)
	{
		selectedIndex = idx;
	}

	private void OnBtnConfirmClick(int arg0)
	{
		int slotIndex;
		EnvOptimizerComponentSlot slot;
		if (!envOptimizerSystem.CanActive(out var isEnergyEnough, out var isComponentEnough))
		{
			if (!isComponentEnough)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiEnvOptimizerTipNoComponent);
			}
			else if (!isEnergyEnough)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiEnvOptimizerTipNoEnergy);
			}
		}
		else if (envOptimizerSystem.TryActiveNext(out slotIndex, out slot))
		{
			latestActivatedComponent = slot.itemName;
			ignoreInput = true;
			base.panel.PlayActiveAnim(slotIndex, new EnvOptimizerConsoleBlockData(slot), delegate
			{
				ignoreInput = false;
				gameController.PopState();
			});
		}
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (!ignoreInput)
		{
			if (userInput.BaseScrollDir.magnitude > 0f)
			{
				_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
			}
			if (userInput.BaseIsCancelPressed)
			{
				gameController.PopState();
			}
		}
	}

	protected override void Show()
	{
		base.Show();
		selectedIndex = 0;
		latestActivatedComponent = "";
		RefreshView();
		base.panel.Show();
		DolocAPI.DelayFrame(delegate
		{
			base.panel.Select(0);
		});
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	private void RefreshView(bool onlyItemSlot = false)
	{
		if (onlyItemSlot)
		{
			base.panel.RenderItemSlotOnly(new EnvOptimizerData(envOptimizerSystem));
		}
		else
		{
			base.panel.Render(new EnvOptimizerData(envOptimizerSystem));
		}
	}

	public override void OnPause()
	{
		base.OnPause();
		Unregister();
	}

	public override void OnResume()
	{
		base.OnResume();
		Register();
		base.panel.Select(selectedIndex);
		RefreshView();
	}
}
