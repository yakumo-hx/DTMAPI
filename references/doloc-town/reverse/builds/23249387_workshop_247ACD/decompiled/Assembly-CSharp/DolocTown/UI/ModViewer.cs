using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ModViewer : DolocUiObject, IScrollContentRect, INavPanel
{
	[SerializeField]
	private Text textTitle;

	[SerializeField]
	private Text textSource;

	[SerializeField]
	private Text textVersion;

	[SerializeField]
	private Text textAuthor;

	[SerializeField]
	private Text textContent;

	[SerializeField]
	private ModFunctionButtonGroup slotButtonGroup;

	[SerializeField]
	public ModFunctionButton switchButton;

	[SerializeField]
	public ModFunctionButton moveUpButton;

	[SerializeField]
	public ModFunctionButton moveDownButton;

	[SerializeField]
	private ModFunctionButtonGroup localButtonGroup;

	[SerializeField]
	public ModFunctionButton openLocalButton;

	[SerializeField]
	public ModFunctionButton uploadButton;

	[SerializeField]
	private ModFunctionButtonGroup workshopButtonGroup;

	[SerializeField]
	public ModFunctionButton workshopButton;

	[SerializeField]
	private ModFunctionButtonGroup emptyHintButtonGroup;

	[SerializeField]
	public ModFunctionButton openLocalRootButton;

	[SerializeField]
	public ModFunctionButton workshopHomepageButton;

	[SerializeField]
	private ScrollRect _scrollRect;

	[SerializeField]
	private CanvasGroup content;

	[SerializeField]
	private Text emptyHint;

	private List<ModFunctionButton> funcButtons = new List<ModFunctionButton>();

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta => 0.05f;

	public Selectable[] allSelectablesArray => ((IEnumerable<Selectable>)(from x in funcButtons
		where x.interactable
		select x.button)).ToArray();

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		funcButtons.Add(switchButton);
		funcButtons.Add(moveUpButton);
		funcButtons.Add(moveDownButton);
		funcButtons.Add(openLocalButton);
		funcButtons.Add(uploadButton);
		funcButtons.Add(workshopButton);
		funcButtons.Add(openLocalRootButton);
		funcButtons.Add(workshopHomepageButton);
		foreach (ModFunctionButton funcButton in funcButtons)
		{
			funcButton.Init();
		}
	}

	public void Render(ModData data)
	{
		if (data != null && data.notEmpty)
		{
			SetText(emptyHint, string.Empty);
			SetText(textTitle, data.name);
			SetText(textSource, (data.sourceType == ModSourceType.Local) ? base.staticTexts.UiModSourceLocal : base.staticTexts.UiModSourceWorkshop);
			SetText(textVersion, data.version);
			SetText(textAuthor, data.author);
			SetText(textContent, data.description);
			switch (data.sourceType)
			{
			case ModSourceType.Local:
				localButtonGroup.SetVisibleAndInteractable(value: true);
				workshopButtonGroup.SetVisibleAndInteractable(value: false);
				uploadButton.buttonText = (data.canUpdateWorkshopItem ? base.staticTexts.UiModUpdateMod : base.staticTexts.UiModUploadMod);
				break;
			case ModSourceType.Workshop:
				localButtonGroup.SetVisibleAndInteractable(value: false);
				workshopButtonGroup.SetVisibleAndInteractable(value: true);
				break;
			}
			slotButtonGroup.SetVisibleAndInteractable(value: true, setInteractable: false);
			switchButton.buttonText = (data.enabled ? base.staticTexts.UiModDisable : base.staticTexts.UiModEnable);
			switchButton.interactable = true;
			moveUpButton.interactable = data.canMoveUp;
			moveDownButton.interactable = data.canMoveDown;
		}
	}

	public void SetEmpty(bool value)
	{
		content.blocksRaycasts = !value;
		content.alpha = ((!value) ? 1 : 0);
		SetText(emptyHint, value ? base.staticTexts.UiModNoMod : string.Empty);
		emptyHintButtonGroup.SetVisibleAndInteractable(value);
		openLocalRootButton.buttonText = base.staticTexts.UiModOpenLocalDirectory;
		workshopHomepageButton.buttonText = base.staticTexts.UiModOpenWorkshop;
		if (value)
		{
			slotButtonGroup.SetVisibleAndInteractable(value: false);
			localButtonGroup.SetVisibleAndInteractable(value: false);
			workshopButtonGroup.SetVisibleAndInteractable(value: false);
		}
		RefreshNavigation();
	}

	public void OnMove()
	{
	}

	public void RefreshNavigation()
	{
		this.RebuildNavigation(allSelectablesArray, 0.1f, 90f, wrapAround: false);
	}
}
