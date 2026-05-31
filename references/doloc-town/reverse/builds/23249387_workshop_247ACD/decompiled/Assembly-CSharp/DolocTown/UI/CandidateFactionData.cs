using DolocTown.Config;
using DolocTown.Config.Mission;
using UnityEngine;

namespace DolocTown.UI;

public struct CandidateFactionData : IUIData
{
	public bool notEmpty { get; }

	public bool isVisited { get; }

	public string npcName { get; }

	public string title { get; }

	public Sprite icon { get; }

	public CandidateFactionData(string factionName)
	{
		this = default(CandidateFactionData);
		notEmpty = false;
		TreatyPortFactionInfo byId = DolocConfig.Tables.TbTreatyPortFaction.GetById(factionName);
		if (byId != null)
		{
			notEmpty = true;
			isVisited = DolocAPI.IsVisitedNpcName(byId.Contact);
			npcName = DolocAPI.GetNpcTitle(byId.Contact);
			title = (isVisited ? byId.Title : DolocConfig.StaticTexts.TreatyPortFactionLock);
			if (!byId.UnlockInDemo)
			{
				title = DolocConfig.StaticTexts.TreatyPortFactionUnopen.Colored(Color.black);
			}
			icon = byId.UiSpriteAsset.Asset;
		}
	}
}
