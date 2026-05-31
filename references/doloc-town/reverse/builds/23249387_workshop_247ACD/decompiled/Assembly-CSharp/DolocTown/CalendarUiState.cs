using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Calendar;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class CalendarUiState : DolocUiState<CalendarPanel>
{
	private List<CalendarDateInfo> _currentMonthData;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private CalendarManager CalendarManager => DolocAPI.archiveHandle.cityData.calendarManager;

	private int currentIndex => base.panel.dateList.selectedIndex;

	private int Month => DolocAPI.archiveHandle.DateNow.Month;

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		_currentMonthData = DolocConfig.Tables.TbCalendar.Get(Month).Date.ToList();
	}

	protected override void Register()
	{
		base.panel.dateList.SetSelectCallbacks(OnDataSelect);
	}

	protected override void Unregister()
	{
		base.panel.dateList.RemoveCallbacks();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseSortItem)
		{
			OpenInputBox();
		}
		else if (userInput.BaseDisposeItem)
		{
			int day = _currentMonthData[currentIndex].Day;
			if (CalendarManager.QueryMemoByDay(day, out var _))
			{
				DolocAPI.ShowQuestionBox(DolocConfig.StaticTexts.CalendarPanelDelMemoHint, delegate
				{
					CalendarManager.DelMemo(day);
					RefreshDateList();
					base.panel.dateList.GetFocus();
				});
			}
		}
		else if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		base.panel.SetTimeTitle(dateNow.YearShown, dateNow.Month);
		RefreshDateList();
		base.panel.Show();
		base.panel.dateList.Select(DolocAPI.archiveHandle.DateNow.Day - 1);
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	public override void OnResume()
	{
		base.OnResume();
		base.panel.dateList.GetFocus();
	}

	private void RefreshDateList()
	{
		List<DateData> list = new List<DateData>();
		foreach (CalendarDateInfo currentMonthDatum in _currentMonthData)
		{
			list.Add(new DateData(Month, currentMonthDatum));
		}
		base.panel.dateList.RefreshView(list.ToArray());
	}

	private void OnDataSelect(int index)
	{
		int day = _currentMonthData[index].Day;
		base.panel.viewer.Render(new DateEventData(Month, day));
		base.panel.operationTip.SetTextKey((!CalendarManager.QueryMemoByDay(day, out var _)) ? new string[1] { base.staticTexts.UiTipAddMemo } : new string[2]
		{
			base.staticTexts.UiTipChangeMemo,
			base.staticTexts.UiTipDelMemo
		});
		DolocAPI.UIRaiseRoll();
	}

	private void OpenInputBox()
	{
		int day = _currentMonthData[currentIndex].Day;
		DolocAPI.EnterUI((LongTextInputUiState state) => state.HandleStartUpArgs(base.staticTexts.CalendarPanelMemoTitle, DolocAPI.QueryMemoByDay(day), delegate(string content)
		{
			CalendarManager.AddMemo(day, content);
			RefreshDateList();
		}, DolocAPI.GlobalParameter.InputMemoMaxLength));
	}
}
