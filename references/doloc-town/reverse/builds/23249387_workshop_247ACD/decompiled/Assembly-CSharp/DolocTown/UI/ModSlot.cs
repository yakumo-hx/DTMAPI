using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ModSlot : DolocNavigationButton
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private TextMeshProUGUI txtDescription;

	[SerializeField]
	public DolocNavigationButton switchButton;

	[SerializeField]
	private DolocNavigationButton moveUpButton;

	[SerializeField]
	private DolocNavigationButton moveDownButton;

	[SerializeField]
	private Transform enabledTip;

	[SerializeField]
	private Transform arrow;

	[SerializeField]
	private Transform workshopIcon;

	[SerializeField]
	private Image iconBackground;

	[SerializeField]
	private Color normalBgColor;

	[SerializeField]
	private Color highlightedBgColor;

	[SerializeField]
	private Color normalIconColor;

	[SerializeField]
	private Color highlightedIconColor;

	public Action<int> onToggleSwitch;

	public Action<int> onMoveUp;

	public Action<int> onMoveDown;

	protected override void __Init()
	{
		base.__Init();
		switchButton.Init();
		switchButton.onClick.AddListener(delegate
		{
			onToggleSwitch?.Invoke(index);
		});
		moveUpButton.Init();
		moveUpButton.onClick.AddListener(delegate
		{
			onMoveUp?.Invoke(index);
		});
		moveDownButton.Init();
		moveDownButton.onClick.AddListener(delegate
		{
			onMoveDown?.Invoke(index);
		});
	}

	public void Render(ModData data)
	{
		SetSprite(iconImg, data.icon);
		txtTitle.text = data.name;
		txtDescription.text = data.description.ClearLineBreaks();
		enabledTip.gameObject.SetActive(data.enabled);
		moveUpButton.interactable = data.canMoveUp;
		moveDownButton.interactable = data.canMoveDown;
		workshopIcon.gameObject.SetActive(data.sourceType == ModSourceType.Workshop);
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		base.highLighted = true;
		arrow.gameObject.SetActive(value: true);
	}

	protected override void OnDeselect()
	{
		base.OnDeselect();
		arrow.gameObject.SetActive(value: false);
	}

	private void OnDisable()
	{
		base.highLighted = false;
		arrow.gameObject.SetActive(value: false);
	}

	protected override void OnHighLighted(bool value)
	{
		base.OnHighLighted(value);
		base.backgroundColor = (value ? highlightedBgColor : normalBgColor);
		iconBackground.color = (value ? highlightedIconColor : normalIconColor);
	}
}
