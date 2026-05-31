using DolocTown.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class GameDataPanel : DolocGridUI<GameDataSlot>
{
	[SerializeField]
	private Text title;

	[SerializeField]
	public DolocButtonComponent btnDelete;

	[SerializeField]
	public DolocButtonComponent btnCopy;

	[SerializeField]
	public DolocButtonComponent btnConfirm;

	[SerializeField]
	private Text confirmText;

	[SerializeField]
	private Text versionText;

	private Color btnNormalColor;

	private Color btnSelectedColor;

	public DolocButtonComponent[] functionButtons => new DolocButtonComponent[3] { btnDelete, btnCopy, btnConfirm };

	public bool focusedOnFunctionButtons { get; private set; }

	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_DATA_SLOT);

	protected override void __Init()
	{
		base.__Init();
		ColorBlock colors = btnConfirm.colors;
		btnNormalColor = colors.normalColor;
		btnSelectedColor = colors.selectedColor;
		SetButtonHighLighted(btnConfirm, value: true);
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
		DolocButtonComponent[] array = functionButtons;
		foreach (DolocButtonComponent button in array)
		{
			button.onMove.AddListener(delegate(MoveDirection dir)
			{
				if (dir == MoveDirection.Up || dir == MoveDirection.Down)
				{
					GetSlot(base.selectedIndex).Select();
				}
			});
			button.onSelect.AddListener(delegate
			{
				focusedOnFunctionButtons = true;
				this.HideItemBorder();
				button.GetComponent<RectTransform>().GetItemBorder(BorderType.Arrow);
				GetSlot(base.selectedIndex).highLighted = true;
				HighLightButton(button);
			});
			button.onDeselect.AddListener(delegate
			{
				focusedOnFunctionButtons = false;
				foreach (GameDataSlot slot in base.slots)
				{
					slot.highLighted = false;
				}
			});
		}
	}

	public void Render(BaseArchiveData[] infos, bool showVersionTip)
	{
		int num = infos.Length;
		SetCapacity(num);
		for (int i = 0; i < num; i++)
		{
			base.slots[i].Render(infos[i]);
		}
		versionText.gameObject.SetActive(showVersionTip);
	}

	public void Render(int index, BaseArchiveData info)
	{
		if (index >= 0 && index < base.slotCount)
		{
			base.slots[index].Render(info);
		}
	}

	public void HighLightButton(DolocButtonComponent button)
	{
		DolocButtonComponent[] array = functionButtons;
		foreach (DolocButtonComponent dolocButtonComponent in array)
		{
			SetButtonHighLighted(dolocButtonComponent, dolocButtonComponent == button);
		}
	}

	private void SetButtonHighLighted(DolocButtonComponent button, bool value)
	{
		ColorBlock colors = button.colors;
		Color highlightedColor = (colors.normalColor = (value ? btnSelectedColor : btnNormalColor));
		colors.highlightedColor = highlightedColor;
		button.colors = colors;
	}

	private void SetCapacity(int totalCapacity)
	{
		int num = totalCapacity / 5 + 1;
		base.SetCapacity(totalCapacity, num);
		BuildNavigation();
		for (int i = 0; i < base.slotCount; i++)
		{
			base.slots[i].SetIndex(i);
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		panelCanvasGroup.blocksRaycasts = true;
		title.text = DolocConfig.StaticTexts.GameDataPanelTitle;
	}

	protected override void OnStartHide()
	{
		panelCanvasGroup.blocksRaycasts = false;
		focusedOnFunctionButtons = false;
		this.HideItemBorder();
		base.OnStartHide();
	}

	public void SetConfirmText(string text)
	{
		confirmText.text = text ?? "";
	}
}
