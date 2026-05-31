using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DolocTown.UI;

public class RebindActionUI : SettingItemUI
{
	[SerializeField]
	private RebindActionSlot btnRebind;

	[SerializeField]
	private RebindActionSlot btnAdditional;

	[SerializeField]
	private DolocNavigationButton btnRevert;

	private DolocButtonComponent backgroundButton;

	public override Selectable selectable => btnRebind.button;

	private DolocInputSource actionAsset => DolocAPI.UserInput.inputSource;

	public InputSchemaType currentInputSchema { get; private set; }

	public DolocInputDeviceType currentDeviceType { get; private set; }

	public RebindActionSlot[] rebindSlots => new RebindActionSlot[2] { btnRebind, btnAdditional };

	public RebindActionMask mask { get; set; }

	public ItemBorder arrow { get; set; }

	public OperationTipInUI operationTip { get; set; }

	public RebindActionSettingComponent rebindActionData => base.config?.Component as RebindActionSettingComponent;

	public RebindActionInfo rebindActionInfo => rebindActionData?.InputAction_Ref;

	public override bool shouldShowArrow => false;

	public bool AllowEmpty
	{
		get
		{
			if (rebindActionInfo != null && !rebindActionInfo.AllowEmpty)
			{
				return btnRebind.OriginPath.IsNullOrEmpty();
			}
			return true;
		}
	}

	public static RebindActionUI currentRebindActionUI { get; private set; }

	private static RebindActionSlot currentSlot { get; set; }

	protected override void __Init()
	{
		base.__Init();
		backgroundButton = GetComponent<DolocButtonComponent>();
		backgroundButton.onClick.AddListener(selectable.Select);
		btnRevert.Init();
		btnRevert.interactable = false;
		RebindActionSlot[] array = rebindSlots;
		foreach (RebindActionSlot btn in array)
		{
			btn.Init();
			btn.onClick.AddListener(delegate
			{
				if (btn.IsLocked)
				{
					DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.SettingPanelCanNotEdit);
				}
				else
				{
					btn.StartInteractiveRebind();
				}
			});
			btn.startRebindEvent.AddListener(delegate
			{
				EventSystem.current?.SetSelectedGameObject(null);
				btn.HideItemBorder();
			});
			btn.updateBindingUIEvent.AddListener(delegate
			{
				bool interactable = rebindSlots.Any((RebindActionSlot x) => x.CanRevert);
				btnRevert.interactable = interactable;
				DolocAPI.DelayFrame(BuildNavigation);
			});
			btn.stopRebindEvent.AddListener(delegate
			{
				btn.Select();
			});
			btn.onSelect.AddListener(delegate
			{
				currentSlot = btn;
				currentRebindActionUI = this;
				arrow.HoverTo(btn, useAnimation: false, BorderType.Arrow);
				operationTip.SetTextKey(AllowEmpty ? base.staticTexts.SettingPanelDelete : "");
			});
			btn.onDeselect.AddListener(delegate
			{
				if (currentSlot == btn)
				{
					currentSlot = null;
				}
				if (currentRebindActionUI == this)
				{
					currentRebindActionUI = null;
				}
				operationTip.SetTextKey("");
			});
		}
		btnRevert.onClick.AddListener(ResetToDefault);
		btnRevert.onSelect.AddListener(delegate
		{
			arrow.HoverTo(btnRevert, useAnimation: false, BorderType.Arrow);
		});
		btnRevert.onDeselect.AddListener(delegate
		{
			btnRevert.HideItemBorder();
		});
	}

	private void BuildNavigation()
	{
		List<Selectable> list = new List<Selectable> { btnRebind.button };
		Navigation navigation = btnRebind.button.navigation;
		if (btnAdditional.interactable)
		{
			list.Add(btnAdditional.button);
			btnAdditional.button.SetNavigation(navigation.selectOnUp, navigation.selectOnDown);
		}
		if (btnRevert.interactable)
		{
			list.Add(btnRevert.button);
			btnRevert.button.SetNavigation(navigation.selectOnUp, navigation.selectOnDown);
		}
		Selectable[] array = list.ToArray();
		array.RebuildNavigationHorizontal(array);
	}

	private void ShowRevertHint(int _)
	{
		if (btnRevert.interactable)
		{
			btnRevert.HoverTextSmall(base.staticTexts.SettingPanelResetToDefault);
		}
	}

	public void RefreshKeyInfo(InputSchemaType inputSchema, DolocInputDeviceType deviceType)
	{
		currentInputSchema = inputSchema;
		currentDeviceType = deviceType;
		RebindActionInfo rebindActionInfo = rebindActionData?.InputAction_Ref;
		if (rebindActionInfo == null || !rebindActionInfo.Valid)
		{
			return;
		}
		InputAction mainAction = rebindActionInfo.MainAction;
		List<InputAction> linkedAction = rebindActionInfo.LinkedAction;
		List<int> bindingIndexes = rebindActionInfo.GetBindingIndexes(inputSchema);
		if (bindingIndexes.IsNullOrEmpty())
		{
			SetVisible(value: false);
			return;
		}
		SetVisible(value: true);
		bool isLocked = currentInputSchema switch
		{
			InputSchemaType.KeyboardMouse => rebindActionInfo.LockMianInKeyboradMouseMode, 
			InputSchemaType.GamePad => rebindActionInfo.LockMianInGamepadMode, 
			_ => false, 
		};
		btnRebind.IsLocked = isLocked;
		btnRebind.BindAction(mainAction, linkedAction.ToArray(), bindingIndexes[0], this, mask);
		if (bindingIndexes.Count < 2)
		{
			btnAdditional.SetActive(value: false);
			btnRebind.button.SetNavigation();
		}
		else
		{
			btnAdditional.SetActive(value: true);
			btnAdditional.BindAction(mainAction, linkedAction.ToArray(), bindingIndexes[1], this, mask);
			DolocAPI.DelayFrame(BuildNavigation);
		}
	}

	private void ResetToDefault(int _)
	{
		btnRebind.ResetToDefault();
		btnAdditional.ResetToDefault();
		btnRebind.Select();
	}

	public void RemoveBind()
	{
		if (currentSlot == btnRebind && (currentSlot.IsLocked || !AllowEmpty))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.SettingPanelCanNotRemove);
		}
		else
		{
			currentSlot.RemoveBind();
		}
	}
}
