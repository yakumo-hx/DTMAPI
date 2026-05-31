using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Weather;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class WeatherReportPanel : DolocUIPanel, INavPanel
{
	private WeatherGroup[] weatherGroups;

	private int dayCount => weatherGroups.Length;

	public Selectable[] allSelectablesArray
	{
		get
		{
			List<Selectable> list = new List<Selectable>();
			WeatherGroup[] array = weatherGroups;
			foreach (WeatherGroup weatherGroup in array)
			{
				list.AddRange(weatherGroup.ActiveSlots.Select((WeatherSlot x) => x.button));
			}
			return list.ToArray();
		}
	}

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		weatherGroups = GetComponentsInChildren<WeatherGroup>(includeInactive: true);
		WeatherGroup[] array = weatherGroups;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Init();
		}
	}

	public void RefreshAndShow()
	{
		for (int i = 0; i < dayCount; i++)
		{
			WeatherGroup weatherGroup = weatherGroups[i];
			WeatherInfo[] weatherInfoOfDay = DolocAPI.archiveHandle.timeData.GetWeatherInfoOfDay(i);
			weatherGroup.Render(weatherInfoOfDay, i == 0, i == 0);
			if (i > 2)
			{
				weatherGroup.SetDayInfo(DolocAPI.archiveHandle.DateNow.GetWeekDayOfDay(i));
			}
		}
		Show(useTween: true, RebuildNavigation);
		SelectCurrent();
		RebuildNavigation();
		DolocAPI.DelayFrame(RebuildNavigation);
	}

	private void SelectCurrent()
	{
		int hour = DolocAPI.archiveHandle.DateNow.Hour;
		int num = ((hour >= 6) ? ((hour < 18) ? 1 : 2) : 0);
		int num2 = num;
		WeatherSlot[] activeSlots = weatherGroups[0].ActiveSlots;
		WeatherSlot weatherSlot = activeSlots[0];
		for (int num3 = activeSlots.Length - 1; num3 >= 0; num3--)
		{
			if (activeSlots[num3].index <= num2)
			{
				weatherSlot = activeSlots[num3];
				break;
			}
		}
		EventSystem.current?.SetSelectedGameObject(null);
		weatherSlot.Select();
		weatherSlot.highLighted = true;
	}

	private void RebuildNavigation()
	{
		this.RebuildNavigationHorizontal(allSelectablesArray);
		this.RebuildNavigationVertical(allSelectablesArray);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		EventSystem.current?.SetSelectedGameObject(null);
		DolocAPI.HideHoverBox();
	}
}
