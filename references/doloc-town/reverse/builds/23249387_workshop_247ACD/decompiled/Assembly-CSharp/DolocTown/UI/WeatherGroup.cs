using System.Linq;
using DolocTown.Config.Weather;
using UnityEngine.UI;

namespace DolocTown.UI;

public class WeatherGroup : DolocUiObject
{
	public Text dayInfo;

	private WeatherSlot[] slots;

	public WeatherSlot[] ActiveSlots => slots.Where((WeatherSlot x) => x.gameObject.activeSelf).ToArray();

	protected override void __Init()
	{
		base.__Init();
		slots = GetComponentsInChildren<WeatherSlot>(includeInactive: true);
		int num = 0;
		WeatherSlot[] array = slots;
		foreach (WeatherSlot obj in array)
		{
			obj.Init();
			obj.index = num++;
			obj.onDeselect.AddListener(delegate
			{
				DolocAPI.HideHoverBox();
			});
		}
	}

	public void Render(WeatherInfo[] infos, bool showFirst, bool showTimeInfo)
	{
		if (infos.IsNullOrEmpty())
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < slots.Length; i++)
		{
			WeatherSlot weatherSlot = slots[i];
			weatherSlot.transform.SetSiblingIndex(i);
			weatherSlot.onPointerEnter.RemoveAllListeners();
			weatherSlot.onSelect.RemoveAllListeners();
			if (i < infos.Length && (DolocAPI.gameManager.gameInitConfig.showAllWeatherReport || showFirst || i > 0))
			{
				WeatherInfo weatherInfo = infos[i];
				if (!DolocAPI.gameManager.gameInitConfig.showAllWeatherReport && i > 0 && weatherInfo.Id == infos[i - 1].Id)
				{
					slots[i].gameObject.SetActive(value: false);
					continue;
				}
				SetSlotActive(weatherSlot, weatherInfo);
				num++;
				weatherSlot.SetTimeInfoVisible(showTimeInfo);
			}
			else
			{
				slots[i].gameObject.SetActive(value: false);
			}
		}
		if (num == 0)
		{
			SetSlotActive(slots[0], infos[0]);
			slots[0].SetTimeInfoVisible(showTimeInfo);
		}
	}

	public void SetDayInfo(string text)
	{
		dayInfo.text = text;
	}

	private void SetSlotActive(WeatherSlot slot, WeatherInfo info)
	{
		slot.highLighted = false;
		slot.iconSprite = info.UiSpriteAsset.Asset;
		slot.onPointerEnter.AddListener(delegate
		{
			slot.Select();
		});
		slot.onSelect.AddListener(delegate
		{
			HoverText(slot, info.Title);
		});
		slot.gameObject.SetActive(value: true);
	}

	private void HoverText(DolocNavigationButton slot, string text)
	{
		slot.HoverTextSmall(text.Colored(DolocUiColor.SLIENTCOLOR_BLUE), UIAlignmentType.BottomMiddle, UIAlignmentType.TopMiddle);
	}
}
