using DolocTown.Config;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct TechPointData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public string title { get; }

	public string description { get; }

	public string progressInfo { get; }

	public float expProgress { get; }

	public TechPointData(TechLevelData levelData)
	{
		this = default(TechPointData);
		if (levelData != null)
		{
			notEmpty = true;
			TechPointInfo proto = levelData.proto;
			title = $"{proto.Title.Colored(DolocUiColor.SLIENTCOLOR_GREEN)} Lv{levelData.CurrentLevel}";
			description = proto.Description;
			progressInfo = (levelData.IsFullyMax ? DolocConfig.StaticTexts.TechtreeMaxLevel.Colored(DolocUiColor.SLIENTCOLOR_RED) : DolocUtils.Format(DolocConfig.StaticTexts.TechtreeProgressInfo, levelData.ExpProgressString.Colored(DolocUiColor.SLIENTCOLOR_GREEN), levelData.PointsForNextLevel.ToString().Colored(DolocUiColor.SLIENTCOLOR_BLUE)));
			icon = proto.Icon.Asset;
			expProgress = levelData.ExpProgress;
		}
	}
}
