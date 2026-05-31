using System;
using System.Linq;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.Fishing;
using DolocTown.Config.Time;
using DolocTown.Config.UI;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct FishDetailData : IUIData
{
	public bool notEmpty { get; }

	public bool isVisited { get; }

	public bool display { get; }

	public Sprite icon { get; }

	public string title { get; }

	public string countText { get; }

	public string descText { get; }

	public string[] itemNames { get; }

	public Sprite[] itemIcons { get; }

	public string documentContent { get; }

	public FishDetailData(string fishName)
	{
		this = default(FishDetailData);
		Item item = DolocAPI.GenerateItem(fishName);
		if (item == null)
		{
			return;
		}
		notEmpty = true;
		DolocAPI.QueryFishDocument(fishName, out var document);
		display = document.Display;
		if (!DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Creature, fishName, out var record))
		{
			return;
		}
		isVisited = record.isUnlock;
		icon = item.uiSprite;
		title = item.title;
		countText = (DolocConfig.Tables.TbFish.DataMap.ContainsKey(fishName) ? DolocConfig.StaticTexts.CollectionPanelFishCatch.Format(DolocAPI.GetEventTriggerCount(GameEventType.FISHING_CATCH_FISH, fishName) + DolocAPI.GetEventTriggerCount(GameEventType.CATCH_FISH_BY_HAT, fishName)) : DolocConfig.StaticTexts.CollectionPanelAnimalBreed.Format(DolocAPI.GetEventTriggerCount(GameEventType.FRY_GROW_UP, fishName)));
		descText = item.description;
		FishInfo orDefault = DolocConfig.Tables.TbFish.GetOrDefault(fishName);
		itemNames = Array.Empty<string>();
		itemIcons = Array.Empty<Sprite>();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(DolocUtils.Format(DolocConfig.StaticTexts.CollectionPanelFishPlace, document.Habitat));
		stringBuilder.Append("\n\n");
		if (orDefault != null)
		{
			itemNames = orDefault.FishBait;
			itemIcons = itemNames.Select(DolocAPI.GetItemSprite).ToArray();
			int[] source = ((orDefault.Month.Length == 0) ? DolocConfig.Tables.TbSeason.DataList.Select((SeasonInfo x) => x.Month).ToArray() : orDefault.Month);
			stringBuilder.Append(DolocUtils.Format(DolocConfig.StaticTexts.CollectionPanelFishMonth, source.Select((int month) => DolocAPI.HandleTimeString(DolocConfig.StaticTexts.UiTextMonths, month)).HandleJoinString()));
			stringBuilder.Append("\n\n");
			stringBuilder.Append(DolocUtils.Format(DolocConfig.StaticTexts.CollectionPanelFishWeather, (orDefault.WeatherTypes.Length == 0) ? DolocConfig.StaticTexts.CollectionPanelFishWeatherNone : orDefault.WeatherTypes.Select((WeatherType type) => DolocConfig.Tables.TbWeather.GetWeatherInfo(type).Title).HandleJoinString()));
			stringBuilder.Append("\n\n");
		}
		DocumentNodeInfo[] documentInfos = document.DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			if (record.documents.Contains(documentNodeInfo.Id))
			{
				stringBuilder.Append(documentNodeInfo.DescriptionAppend);
				stringBuilder.Append("\n\n\n");
			}
		}
		documentContent = stringBuilder.ToString();
	}
}
