using UnityEngine;

namespace DolocTown.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DolocBasicTip : DolocUiObject
{
	protected CanvasGroup canvasGroup;

	private SimpleGroup parentGroup;

	private DolocButtonComponent button;

	[HideInInspector]
	public bool showBySetting;

	protected override void __Init()
	{
		base.__Init();
		canvasGroup = base.gameObject.GetOrCreateComponent<CanvasGroup>();
		button = base.gameObject.GetOrCreateButton();
		button.onClick.AddListener(OnClick);
		button.onPointerEnter.AddListener(OnHover);
		button.onPointerExit.AddListener(this.HideHoverBox);
	}

	protected virtual void OnClick()
	{
	}

	protected virtual void OnHover()
	{
	}

	public override void SetVisible(bool value)
	{
		base.SetVisible(showBySetting && value);
	}

	public void ForceShow()
	{
		canvasGroup.ignoreParentGroups = true;
		SetVisible(value: true);
		if (parentGroup != null)
		{
			parentGroup.UpdateVisibleState();
		}
	}

	public void ResetCanvasGroup()
	{
		canvasGroup.ignoreParentGroups = false;
	}

	private void OnDisable()
	{
		if (canvasGroup != null)
		{
			canvasGroup.ignoreParentGroups = false;
		}
		if (parentGroup != null)
		{
			parentGroup.UpdateVisibleState();
		}
	}
}
