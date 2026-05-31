using DolocTown.Config.Settings;
using DolocTown.UI;

namespace DolocTown;

public class SettingPanelUiState : DolocUiState<SettingPanel>
{
	private UserSettings backupSettings;

	private string backupInputOverload;

	private int maxIndex = 5;

	public override bool PermanentState => true;

	private IScrollContentRect _contentRect => base.panel;

	private UserSettings userSettings => DolocAPI.userSettings;

	public bool HandleStartUpArgs(bool useRaycastMask)
	{
		base.panel.useRaycastMask = useRaycastMask;
		return true;
	}

	protected override void OnInit()
	{
		base.OnInit();
		DolocAPI.RegisterMsgListener(UserSettingType.LANGUAGE_TEXT, delegate
		{
			RefreshView();
		});
		DolocAPI.RegisterMsgListener(UserSettingType.TIP_ALL, delegate(object _, GameEventArgs args)
		{
			bool flag2 = (bool)((GameEventArgs<object>)args).value;
			UserSettingType[] allSettings = DolocAPI.uiSystem.basicTip.allSettings;
			foreach (UserSettingType id in allSettings)
			{
				base.panel.GetItemCmp<ToggleItemUI>(id, out var item2);
				if (item2.currentValue != flag2)
				{
					item2.FireClick(select: false);
				}
			}
			base.panel.GetItemCmp<ToggleItemUI>(UserSettingType.TIP_ALL, out var _);
		});
		DolocAPI.RegisterMsgListener(UserSettingType.OPERATION_ALLOW_QUICK_SYNTHESIS, delegate(object _, GameEventArgs args)
		{
			bool flag = (bool)((GameEventArgs<object>)args).value;
			base.panel.GetItemCmp<OptionItemUI>(UserSettingType.OPERATION_QUICK_SYNTHESIS_CONFLICT, out var item);
			item.SetGrayed(!flag);
		});
		DolocAPI.userSettings.UpdateCachedData();
	}

	private void RefreshView()
	{
		base.panel.Render(userSettings, force: true);
		if (base.panel.GetItemCmp<OptionItemUI>(UserSettingType.OPERATION_QUICK_SYNTHESIS_CONFLICT, out var item))
		{
			item.SetGrayed(!userSettings.GetOrDefault<bool>(UserSettingType.OPERATION_ALLOW_QUICK_SYNTHESIS));
		}
	}

	protected override void Register()
	{
		base.panel.saveBtn.onClick.AddListener(OnSave);
		base.panel.revertBtn.onClick.AddListener(OnRevertDefault);
		base.panel.OnCloseButtonClick.AddListener(TryExit);
		base.panel.titleMenu.SetSelectCallbacks(delegate(int index)
		{
			base.panel.SetSettingGroup(index, force: false);
		});
	}

	protected override void Unregister()
	{
		base.panel.saveBtn.onClick.RemoveListener(OnSave);
		base.panel.revertBtn.onClick.RemoveListener(OnRevertDefault);
		base.panel.OnCloseButtonClick.RemoveListener(TryExit);
		base.panel.titleMenu.RemoveCallbacks();
	}

	private void OnSave()
	{
		if (!base.panel.conflictNow)
		{
			DolocAPI.ShowQuestionBox(base.staticTexts.SettingPanelCanNotSaveByConflict, delegate
			{
				userInput.RevertBindingOverrides(backupInputOverload);
				SaveBindings();
				SaveBaseSettings();
				DolocAPI.ShowMessageBoxSmall(base.staticTexts.SettingPanelSaveSuccessful);
				Quit();
			}, delegate
			{
				SaveBaseSettings();
				DolocAPI.ShowMessageBoxSmall(base.staticTexts.SettingPanelOtherSaveSuccessful);
			});
		}
		else
		{
			SaveBindings();
			SaveBaseSettings();
			DolocAPI.UIRaiseConfirm();
			Quit();
		}
		void Quit()
		{
			gameController.PopState();
			if (gameController.CheckState<MainMenuUiState>())
			{
				gameController.PopState();
			}
		}
		void SaveBaseSettings()
		{
			backupSettings = userSettings.Copy();
			DolocAPI.SaveUserSettings(userSettings);
		}
		void SaveBindings()
		{
			userInput.SaveBindingOverrides();
			backupInputOverload = userInput.GetCurrentOverride();
			userInput.HandleEventInputMapping();
		}
	}

	private void OnRevertDefault()
	{
		if (base.panel.currentGroupId.IsNullOrEmpty())
		{
			return;
		}
		DolocAPI.ShowQuestionBox(base.staticTexts.SettingPanelResetConfirm, delegate
		{
			DolocAPI.RevertUserSettingsToDefaultByGroup(base.panel.currentGroupId);
			if (base.panel.currentGroupId == DolocAPI.GlobalParameter.RebindActionSettingGroup)
			{
				userInput.RevertToDefault();
			}
			RefreshView();
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.SettingPanelReset);
		});
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		DolocAPI.LoadUserSettings();
		backupSettings = userSettings.Copy();
		backupInputOverload = userInput.GetCurrentOverride();
		RefreshView();
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		RefreshView();
	}

	protected override void Hide()
	{
		DolocAPI.userSettings.UpdateCachedData();
		if (DolocAPI.IsDataLoaded)
		{
			DolocAPI.RefreshSunLight(0f);
		}
		base.panel.Hide();
		base.Hide();
	}

	private void TryExit()
	{
		if (base.panel.isRebindWaitingConfirm)
		{
			base.panel.listeningKeyMask.TryCancel();
		}
		else if (DolocButtonComponent.latestClickType != ClickType.Mouse && !base.panel.isSaveButtonSelect)
		{
			base.panel.saveBtn.Select();
		}
		else if (!userSettings.Equals(backupSettings) || backupInputOverload != userInput.GetCurrentOverride())
		{
			DolocAPI.ShowQuestionBox(base.staticTexts.SettingPanelExitConfirm, OnSave, delegate
			{
				userInput.RevertBindingOverrides(backupInputOverload);
				userInput.SaveBindingOverrides();
				DolocAPI.RevertUserSettings(backupSettings);
				RefreshView();
				DolocAPI.ShowMessageBoxSmall(base.staticTexts.SettingPanelReset);
				gameController.PopState();
			});
		}
		else
		{
			gameController.PopState();
		}
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseScrollDir.magnitude > 0f && !base.panel.isRebindWaitingConfirm)
		{
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
		if (userInput.BaseDisposeItem && !base.panel.isRebindWaitingConfirm)
		{
			if (RebindActionUI.currentRebindActionUI != null)
			{
				RebindActionUI.currentRebindActionUI.RemoveBind();
			}
		}
		else if (userInput.BaseIsLastPressed && !base.panel.isRebindWaitingConfirm)
		{
			base.panel.titleMenu.FireClickLeft();
		}
		else if (userInput.BaseIsNextPressed && !base.panel.isRebindWaitingConfirm)
		{
			base.panel.titleMenu.FireClickRight();
		}
		else if (userInput.BaseIsCancelPressed && !base.panel.isRebindPending && !base.panel.isRebindWaitingConfirm)
		{
			TryExit();
		}
	}
}
