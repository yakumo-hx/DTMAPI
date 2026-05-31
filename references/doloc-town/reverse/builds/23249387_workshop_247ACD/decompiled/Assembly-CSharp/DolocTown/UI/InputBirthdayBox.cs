using System;
using DolocTown.GameData;
using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class InputBirthdayBox : DolocUIPanel
{
	[SerializeField]
	private TextMeshProUGUI title;

	[SerializeField]
	private TextMeshProUGUI info;

	[SerializeField]
	private InputNumberSlot monthNumberSlot;

	[SerializeField]
	private InputNumberSlot dayNumberSlot;

	[SerializeField]
	public DolocButtonComponent confirmButton;

	protected Action<int, int> onConfirm;

	public int currentMonth => monthNumberSlot.currentValue;

	public int currentDay => dayNumberSlot.currentValue;

	public InputNumberSlot currentNumberSlot { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		monthNumberSlot.Init();
		dayNumberSlot.Init();
		confirmButton.onClick.AddListener(OnConfirm);
		monthNumberSlot.onPointerEnter.AddListener(delegate
		{
			SelectMonth();
		});
		dayNumberSlot.onPointerEnter.AddListener(delegate
		{
			SelectDay();
		});
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		SelectMonth();
	}

	public void Render(Action<int, int> onConfirm)
	{
		title.text = base.staticTexts.InputTitlePlayerBirthday;
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		info.text = DolocUtils.Format(base.staticTexts.UiTipTimeFormat, dateNow.GetDateSimpleInfo());
		monthNumberSlot.Render(1, DolocAPI.GlobalParameter.Year2Month, dateNow.Month);
		dayNumberSlot.Render(1, DolocAPI.GlobalParameter.Month2Day, dateNow.Day);
		this.onConfirm = onConfirm;
	}

	protected virtual void OnConfirm()
	{
		DolocAPI.ShowQuestionBox(DolocUtils.Format(base.staticTexts.InputBirthdayConfirm, currentMonth.ToString("D2").Colored(DolocUiColor.EYECATCHCOLOR_CYAN), currentDay.ToString("D2").Colored(DolocUiColor.EYECATCHCOLOR_CYAN)), delegate
		{
			onConfirm?.Invoke(currentMonth, currentDay);
		});
	}

	public void SelectMonth()
	{
		currentNumberSlot = monthNumberSlot;
		monthNumberSlot.highLighted = true;
		dayNumberSlot.highLighted = false;
	}

	public void SelectDay()
	{
		currentNumberSlot = dayNumberSlot;
		dayNumberSlot.highLighted = true;
		monthNumberSlot.highLighted = false;
	}

	public void TrySelectConfirm()
	{
		confirmButton.Select();
	}

	public bool AddDiff(int diff)
	{
		return currentNumberSlot.AddDiff(diff);
	}
}
