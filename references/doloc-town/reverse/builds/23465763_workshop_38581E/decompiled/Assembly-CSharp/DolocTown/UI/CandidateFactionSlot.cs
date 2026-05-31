using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CandidateFactionSlot : DolocNavigationButton
{
	[SerializeField]
	private Text npcName;

	[SerializeField]
	private Text factionName;

	public void Render(CandidateFactionData data)
	{
		npcName.text = data.npcName;
		factionName.text = data.title;
		iconImg.sprite = data.icon;
		iconImg.color = (data.isVisited ? Color.white : Color.black);
	}
}
