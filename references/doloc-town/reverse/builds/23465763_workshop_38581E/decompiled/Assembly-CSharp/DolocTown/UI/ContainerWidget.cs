using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ContainerWidget : InventoryPanel
{
	[SerializeField]
	public AutoSizeText info;

	[HideInInspector]
	public new int lineCapacity = 10;

	[SerializeField]
	public ContainerLabelUI containerLabelUI;

	[SerializeField]
	public ContainerColorTagUI containerColorTagUI;

	[SerializeField]
	public ContainerSocketUI containerSocketUI;

	[SerializeField]
	private DolocNavigationButton launchBtn;

	[SerializeField]
	private DolocNavigationButton repairBtn;

	[SerializeField]
	private DolocNavigationButton renameBtn;

	[SerializeField]
	private ConversionRecipeViewer conversionRecipeViewer;

	private Sprite normalLaunchSprite;

	private Action onLaunch;

	private Action onRepair;

	private Action onRename;

	public bool isFunctionButtonSelect { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
		containerLabelUI.Init();
		containerColorTagUI.Init();
		containerSocketUI.Init();
		info.gameObject.SetActive(value: false);
		launchBtn.Init();
		normalLaunchSprite = launchBtn.iconSprite;
		launchBtn.gameObject.SetActive(value: false);
		launchBtn.onPointerEnter.AddListener(delegate
		{
			launchBtn.HoverTextSmall(base.staticTexts.DropoffBoxLaunchDrone);
		});
		launchBtn.onPointerExit.AddListener(delegate
		{
			launchBtn.HideHoverBox();
		});
		launchBtn.onClick.AddListener(delegate
		{
			onLaunch?.Invoke();
		});
		repairBtn.Init();
		repairBtn.gameObject.SetActive(value: false);
		repairBtn.onClick.AddListener(delegate
		{
			onRepair?.Invoke();
		});
		repairBtn.onSelect.AddListener(delegate
		{
			isFunctionButtonSelect = true;
			repairBtn.GetItemBorder();
		});
		repairBtn.onDeselect.AddListener(delegate
		{
			isFunctionButtonSelect = false;
			repairBtn.HideItemBorder();
		});
		SetCloseButtonVisible(value: false);
		conversionRecipeViewer.Init();
		conversionRecipeViewer.SetVisible(value: false);
		renameBtn.Init();
		renameBtn.gameObject.SetActive(value: false);
		renameBtn.onClick.AddListener(delegate
		{
			onRename?.Invoke();
		});
		renameBtn.onSelect.AddListener(delegate
		{
			isFunctionButtonSelect = true;
			renameBtn.GetItemBorder();
		});
		renameBtn.onDeselect.AddListener(delegate
		{
			isFunctionButtonSelect = false;
			renameBtn.HideItemBorder();
		});
	}

	protected override int GetLineCapacity(int total)
	{
		return lineCapacity;
	}

	public void SetInfo(string text)
	{
		info.text = text;
		info.SetVisible(!text.IsNullOrEmpty());
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		SetLaunchCallback(null);
		SetRepairCallback(null);
		SetRenamingCallback(null);
		SetCloseButtonVisible(value: false);
		DisableLaunchButton(value: false);
		conversionRecipeViewer.SetVisible(value: false);
	}

	protected override void OnFinishHide()
	{
		info.gameObject.SetActive(value: false);
		containerLabelUI.Hide();
		containerColorTagUI.Hide();
		containerSocketUI.Hide();
		isFunctionButtonSelect = false;
		base.OnFinishHide();
	}

	public void SetLaunchCallback(Action onLaunch)
	{
		this.onLaunch = onLaunch;
		launchBtn.gameObject.SetActive(onLaunch != null);
	}

	public void DisableLaunchButton(bool value)
	{
		if (value)
		{
			launchBtn.transition = Selectable.Transition.None;
			launchBtn.iconSprite = launchBtn.spriteState.disabledSprite;
		}
		else
		{
			launchBtn.transition = Selectable.Transition.SpriteSwap;
			launchBtn.iconSprite = normalLaunchSprite;
		}
	}

	public void SetRepairCallback(Action onRepair)
	{
		this.onRepair = onRepair;
		repairBtn.gameObject.SetActive(onRepair != null);
	}

	public void FadeRepairSprite(Sprite sprite)
	{
		repairBtn.RaiseUiSpriteFadeUp(sprite);
	}

	public void SetRenamingCallback(Action onRename)
	{
		this.onRename = onRename;
		renameBtn.gameObject.SetActive(onRename != null);
	}

	public void RenderRecipeViewer(ConversionRecipeData data)
	{
		if (!data.notEmpty)
		{
			conversionRecipeViewer.SetVisible(value: false);
			conversionRecipeViewer.RebuildLayout();
		}
		else
		{
			conversionRecipeViewer.Render(data);
			conversionRecipeViewer.SetVisible(value: true);
			conversionRecipeViewer.RebuildLayout();
		}
	}

	protected override Selectable[] GetAllSelectables()
	{
		List<Selectable> list = new List<Selectable>();
		foreach (ItemNavSlot slot in base.slots)
		{
			list.Add(slot.button);
		}
		if (conversionRecipeViewer.gameObject.activeSelf)
		{
			list.Add(conversionRecipeViewer.targetItem.button);
		}
		if (containerSocketUI.gameObject.activeSelf)
		{
			list.AddRange(containerSocketUI.slots.Select((ItemNavSlot slot) => slot.button));
		}
		return list.ToArray();
	}

	public override void BuildNavigation()
	{
		if (conversionRecipeViewer.gameObject.activeSelf || containerSocketUI.gameObject.activeSelf)
		{
			this.RebuildNavigationHorizontal(base.allSelectablesArray);
		}
		else
		{
			base.BuildNavigation();
		}
		DolocNavigationButton dolocNavigationButton = ((onRepair != null) ? repairBtn : ((onRename != null) ? renameBtn : null));
		if (dolocNavigationButton != null)
		{
			ItemNavSlot itemNavSlot = base.slots[^1];
			dolocNavigationButton.button.SetNavigationOnRight(itemNavSlot.button.navigation.selectOnRight);
			dolocNavigationButton.button.SetNavigationOnUp(itemNavSlot.button.navigation.selectOnRight);
			dolocNavigationButton.button.SetNavigationOnDown(itemNavSlot.button.navigation.selectOnRight);
			dolocNavigationButton.button.SetNavigationOnLeft(itemNavSlot.button);
			itemNavSlot.button.SetNavigationOnRight(dolocNavigationButton.button);
		}
	}
}
