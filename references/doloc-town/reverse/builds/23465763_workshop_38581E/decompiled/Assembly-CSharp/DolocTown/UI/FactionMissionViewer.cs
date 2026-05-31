using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class FactionMissionViewer : DolocSinglePageUI<FactionMissionData>, INavPanel, IScrollContentRect
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text sender;

	[SerializeField]
	private Text content;

	[SerializeField]
	private Text costInfo;

	[SerializeField]
	private Text emptyHint;

	[SerializeField]
	private Transform slotRoot;

	[SerializeField]
	private RewardViewer rewardViewer;

	[SerializeField]
	private Transform completeMask;

	[SerializeField]
	private Transform completeIcon;

	[SerializeField]
	private Transform lockMask;

	[SerializeField]
	private Text lockHint;

	[SerializeField]
	public CanvasGroup canvasGroup;

	[SerializeField]
	public ConfirmCraftButton moneyConfirmButton;

	[SerializeField]
	private ScrollRect _scrollRect;

	[HideInInspector]
	public FactionItemSlot[] slots;

	private int activeCount;

	public Selectable[] allSelectablesArray { get; private set; }

	public int allSelectableCount => allSelectablesArray.Length;

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta => 0.05f;

	protected override void __Init()
	{
		base.__Init();
		slots = slotRoot.GetComponentsInChildren<FactionItemSlot>(includeInactive: true);
		int num = 0;
		FactionItemSlot[] array = slots;
		foreach (FactionItemSlot obj in array)
		{
			obj.Init();
			obj.index = num++;
		}
		rewardViewer.Init();
		moneyConfirmButton.Init();
	}

	protected override void Render(FactionMissionData data)
	{
		if (!data.notEmpty)
		{
			SetEmpty(value: true);
			return;
		}
		SetEmpty(value: false);
		SetCompleted(data.isCompleted);
		lockMask.gameObject.SetActive(!data.unlock);
		lockHint.text = data.lockHint;
		title.text = data.title;
		content.text = data.description;
		sender.text = data.sender;
		costInfo.text = data.costInfo;
		List<FactionItemData> items = data.items;
		CheckSlotCount(items.Count);
		for (int i = 0; i < activeCount; i++)
		{
			slots[i].Render(items[i]);
		}
		rewardViewer.Render(data.rewardData, showEmptyInfo: true);
		if (data.moneyCost.notEmpty && data.unlock)
		{
			moneyConfirmButton.SetVisible(value: true);
			moneyConfirmButton.buttonText = data.buttonText;
			moneyConfirmButton.grayed = !data.isMoneyCostEnough;
			moneyConfirmButton.alpha = ((!data.moneyCost.isFinished) ? 1 : 0);
			moneyConfirmButton.interactable = !data.moneyCost.isFinished;
			allSelectablesArray = new Selectable[1] { moneyConfirmButton.button };
		}
		else
		{
			moneyConfirmButton.SetVisible(value: false);
			allSelectablesArray = Array.Empty<Selectable>();
		}
		RebuildLayout();
		scrollRect.verticalScrollbar.value = 1f;
	}

	private void CheckSlotCount(int count)
	{
		activeCount = Mathf.Min(count, slots.Length);
		if (slots.Length < count)
		{
			Debug.LogWarning("传入数据超过ui显示最大容量");
		}
		FactionItemSlot[] array = slots;
		foreach (FactionItemSlot obj in array)
		{
			obj.SetVisible(obj.index < count);
		}
	}

	private void SetCompleted(bool value)
	{
		completeMask.gameObject.SetActive(value);
		completeIcon.gameObject.SetActive(value);
	}

	protected override void SetEmpty(bool value)
	{
		emptyHint.gameObject.SetActive(value);
		canvasGroup.alpha = ((!value) ? 1 : 0);
	}

	public void OnMove()
	{
	}
}
