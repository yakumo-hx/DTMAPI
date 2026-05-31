using System.Linq;
using System.Text;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.UI;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct NpcDetailData : IUIData
{
	public bool notEmpty { get; }

	public bool isVisited { get; }

	public bool display { get; }

	public Sprite icon { get; }

	public string name { get; }

	public int giftCount { get; }

	public string address { get; }

	public bool unLockGift { get; }

	public int likingLevel { get; }

	public int likingMaxLevel { get; }

	public bool showLikingLvHint { get; }

	public string missionHint { get; }

	public string likeContent { get; }

	public string documentContent { get; }

	public NpcDetailData(string npcName)
	{
		this = default(NpcDetailData);
		if (!DolocConfig.Tables.TbNpcDocument.DataMap.TryGetValue(npcName, out var value))
		{
			return;
		}
		notEmpty = true;
		display = value.Display;
		if (!DolocAPI.archiveHandle.GetRecord(CompendiumLabel.Npc, npcName, out var record))
		{
			return;
		}
		isVisited = record.isUnlock;
		icon = value.SceneSpriteAsset.Asset;
		name = DolocAPI.GetNpcTitle(npcName);
		address = string.Format(DolocConfig.StaticTexts.CollectionPanelNpcAddress, value.Address.Colored(DolocUiColor.TEXTCOLOR_STD));
		unLockGift = !DolocConfig.Tables.TbNpc.GetOrDefault(npcName).GiftDialogue.IsNullOrEmpty();
		DolocAPI.QueryNpcLikingInfo(npcName, out var liking);
		giftCount = liking.giftCount;
		likingLevel = liking.LikingLevel;
		likingMaxLevel = DolocAPI.GetNpcLikingLvLimit(npcName);
		if (likingMaxLevel < DolocConfig.Tables.TbGlobalParameter.UpperFavorabilityLevel)
		{
			showLikingLvHint = unLockGift && likingLevel == likingMaxLevel;
		}
		DocumentMissionTip documentMissionTip = value.MissionTips.FirstOrDefault((DocumentMissionTip tip) => DolocAPI.IsMissionListening(tip.Id));
		if (documentMissionTip != null && likingLevel >= documentMissionTip.LikingLevel)
		{
			missionHint = documentMissionTip.Tip;
		}
		string[] source = liking.giftRecord.OrderByDescending((string gift) => DolocAPI.QueryGiftLevel(npcName, gift)).ToArray();
		source = source.Where((string gift) => DolocAPI.QueryGiftLevel(npcName, gift) > 0).Select(DolocAPI.GetItemTitle).ToArray();
		likeContent = ((source.Length != 0) ? string.Format(DolocConfig.StaticTexts.CollectionPanelNpcLikeRecord, source.HandleJoinString().Colored(DolocUiColor.TEXTCOLOR_STD)) : DolocConfig.StaticTexts.CollectionPanelNpcLikeNone.Colored(DolocUiColor.TEXTCOLOR_CYAN));
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		DocumentNodeInfo[] documentInfos = value.DocumentInfos;
		foreach (DocumentNodeInfo documentNodeInfo in documentInfos)
		{
			if (record.documents.Contains(documentNodeInfo.Id))
			{
				stringBuilder.Append(documentNodeInfo.DescriptionAppend);
				stringBuilder.Append("\n\n\n");
				num++;
			}
		}
		string value2 = ((num < value.DocumentInfos.Length) ? string.Format(DolocConfig.StaticTexts.CollectionPanelNpcContentLockTip, name).Colored(DolocUiColor.SLIENTCOLOR_RED) : DolocConfig.StaticTexts.CollectionPanelNpcContentEnd);
		stringBuilder.Append(value2);
		stringBuilder.Append("\n\n");
		documentContent = stringBuilder.ToString();
	}
}
