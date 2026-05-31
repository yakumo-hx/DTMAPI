using System;
using DolocTown.Config;
using DolocTown.Config.Time;
using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CalendarPanel : DolocUIPanel
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Transform monthRoot;

	[SerializeField]
	private Transform weekRoot;

	[SerializeField]
	public CalendarDateList dateList;

	[SerializeField]
	public CalendarViewer viewer;

	private ObjectPool<DolocSimpleText> _monthPool;

	private ObjectPool<DolocSimpleText> _weekPool;

	protected override void __Init()
	{
		base.__Init();
		_monthPool = new ObjectPool<DolocSimpleText>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_MONTH_SLOT), monthRoot, usePreset: true);
		_weekPool = new ObjectPool<DolocSimpleText>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_WEEK_SLOT), weekRoot, usePreset: true);
	}

	public void SetTimeTitle(int year, int month)
	{
		title.text = base.staticTexts.CalendarPanelYearTitle.Format(year);
		_monthPool.CheckCount(DolocAPI.GlobalParameter.Year2Month);
		_monthPool.Sort();
		for (int i = 0; i < _monthPool.ActiveCount; i++)
		{
			SeasonInfo byIndex = DolocConfig.Tables.TbSeason.GetByIndex(i);
			_monthPool[i].Text = byIndex.Title;
			_monthPool[i].Color = ((month == byIndex.Month) ? DolocUiColor.EYECATCHCOLOR_CYAN : DolocUiColor.TEXTCOLOR_STD);
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		Array values = Enum.GetValues(typeof(WeekDay));
		_weekPool.CheckCount(7);
		_weekPool.Sort();
		for (int i = 0; i < values.Length; i++)
		{
			_weekPool[i].Text = DolocConfig.GetEnumText((WeekDay)values.GetValue(i));
		}
	}
}
