using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CalendarViewer : DolocUIPanel
{
	[SerializeField]
	private Text date;

	[SerializeField]
	private Text eventTitle;

	[SerializeField]
	private Transform eventRoot;

	[SerializeField]
	private Text memoTitle;

	[SerializeField]
	private Text memoContent;

	private ObjectPool<DateEventInfo> _slotPool;

	protected override void __Init()
	{
		base.__Init();
		_slotPool = new ObjectPool<DateEventInfo>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_EVENT_SLOT), eventRoot, usePreset: true);
	}

	public void Render(DateEventData data)
	{
		date.text = data.timeStr;
		eventTitle.text = base.staticTexts.CalendarPanelEventTitle;
		memoTitle.text = base.staticTexts.CalendarPanelMemoTitle;
		_slotPool.CheckCount(data.eventsTitle.Length);
		for (int i = 0; i < _slotPool.ActiveCount; i++)
		{
			_slotPool[i].Render(data.eventsTitle[i], data.eventsDesc[i]);
		}
		memoContent.text = (data.memoContent.IsNullOrEmpty() ? base.staticTexts.CalendarPanelEmptyHint : data.memoContent);
		RebuildLayout();
	}
}
