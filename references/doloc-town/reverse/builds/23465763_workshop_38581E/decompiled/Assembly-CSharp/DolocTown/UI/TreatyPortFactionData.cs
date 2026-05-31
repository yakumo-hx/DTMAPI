using DolocTown.Config;
using DolocTown.Config.Mission;
using UnityEngine;

namespace DolocTown.UI;

public struct TreatyPortFactionData : IUIData
{
	public bool notEmpty { get; }

	public FactionType factionType { get; }

	public string factionTitle { get; }

	public string principal { get; }

	public string time { get; }

	public string progress { get; }

	public int maxReputationLv { get; }

	public int reputationLv { get; }

	public bool isComplete { get; }

	public Sprite medal { get; }

	public Sprite npcIcon { get; }

	public Color themeColor { get; }

	public TreatyPortFactionData(FactionType factionType)
	{
		this = default(TreatyPortFactionData);
		notEmpty = false;
		TreatyPortFactionInfo byFactionType = DolocConfig.Tables.TbTreatyPortFaction.GetByFactionType(factionType);
		if (byFactionType != null)
		{
			notEmpty = true;
			this.factionType = factionType;
			factionTitle = byFactionType.Title;
			DolocAPI.QueryTreatyPortFaction(byFactionType.Id, out var faction);
			principal = ((byFactionType.FactionType == FactionType.Doloc) ? DolocUtils.Format(DolocConfig.StaticTexts.TreatyPortPrincipal, DolocAPI.GetNpcTitle(byFactionType.Contact)) : DolocUtils.Format(DolocConfig.StaticTexts.TreatyPortContactNpc, DolocAPI.GetNpcTitle(byFactionType.Contact)));
			time = ((byFactionType.FactionType == FactionType.Doloc) ? DolocUtils.Format(DolocConfig.StaticTexts.TreatyPortRepairTime, faction.SettlementTime) : DolocUtils.Format(DolocConfig.StaticTexts.TreatyPortFactionEnterTime, faction.SettlementTime));
			progress = DolocUtils.Format(DolocConfig.StaticTexts.TreatyPortFactionMissionProgress, $"{faction.CompletedMissionCount}/{faction.TotalMissionCount}");
			maxReputationLv = byFactionType.MedalReputation / 10;
			reputationLv = faction.ReputationValue / 10;
			isComplete = faction.ReputationValue >= byFactionType.MedalReputation;
			medal = byFactionType.Medal.Asset;
			npcIcon = byFactionType.NpcSpriteAsset.Asset;
			themeColor = byFactionType.ThemeColor;
		}
	}
}
