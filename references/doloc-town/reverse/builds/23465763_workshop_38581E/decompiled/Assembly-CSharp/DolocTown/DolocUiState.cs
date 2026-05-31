using DolocTown.Config;
using DolocTown.Config.Localization;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace DolocTown;

public abstract class DolocUiState<TPanel> : DolocGameUiState where TPanel : DolocUIPanel
{
	private GameObject prevSelected;

	protected TPanel panel { get; private set; }

	protected virtual bool RevertPrevSelected => false;

	protected TbStaticText staticTexts => DolocConfig.StaticTexts;

	protected virtual UnityEvent OnCloseButtonClick => null;

	protected virtual bool hideOnPause => false;

	protected virtual bool disablePopUpSound => false;

	protected virtual bool disablePopDownSound => false;

	protected sealed override void OnUiEnter()
	{
		base.OnUiEnter();
		if (RevertPrevSelected && EventSystem.current != null)
		{
			prevSelected = EventSystem.current.currentSelectedGameObject;
			EventSystem.current.SetSelectedGameObject(null);
		}
		BeforeRegister();
		OnCloseButtonClick?.RemoveListener(ClickCloseButton);
		OnCloseButtonClick?.AddListener(ClickCloseButton);
		Register();
	}

	protected virtual void ClickCloseButton()
	{
		gameController.PopState();
	}

	protected override void OnInit()
	{
		base.OnInit();
		panel = DolocAPI.uiSystem.GetEntity<TPanel>(this);
	}

	protected sealed override void OnUiExit()
	{
		OnCloseButtonClick?.RemoveListener(ClickCloseButton);
		Unregister();
		base.OnUiExit();
		if (RevertPrevSelected && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(prevSelected);
		}
		prevSelected = null;
	}

	protected virtual void BeforeRegister()
	{
	}

	protected abstract void Register();

	protected abstract void Unregister();

	public void Kill()
	{
		Unregister();
	}

	public override void OnPause()
	{
		if (hideOnPause)
		{
			Hide();
		}
	}

	public override void OnResume()
	{
		DolocAPI.SetSceneOperationTipEnabled(value: false);
		if (hideOnPause)
		{
			Show();
		}
	}

	protected sealed override void OnUiPause()
	{
	}

	protected sealed override void OnUiResume()
	{
	}

	protected override void Show()
	{
		if (!disablePopUpSound)
		{
			DolocAPI.UIRaisePopUp();
		}
	}

	protected override void Hide()
	{
		if (!disablePopDownSound)
		{
			DolocAPI.UIRaisePopDown();
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (!panel.proto.Resident)
		{
			DolocAPI.uiSystem.TryDestroyEntity<TPanel>(this);
		}
	}

	protected virtual void RefreshTip()
	{
	}

	public override void OnInputDeviceChanged(DolocInputDeviceType type)
	{
		base.OnInputDeviceChanged(type);
		RefreshTip();
		if (panel.operationTip != null)
		{
			panel.operationTip.OnRefresh(type);
		}
		IInputDeviceDetect[] componentsInChildren = panel.GetComponentsInChildren<IInputDeviceDetect>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].OnRefresh(type);
		}
	}
}
