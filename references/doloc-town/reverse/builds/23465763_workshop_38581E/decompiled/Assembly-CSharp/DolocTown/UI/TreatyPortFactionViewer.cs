using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TreatyPortFactionViewer : DolocUiRecyclableObject
{
	[SerializeField]
	private Image bgImage;

	[SerializeField]
	private Text title;

	[SerializeField]
	private Text principal;

	[SerializeField]
	private Text time;

	[SerializeField]
	private Text progress;

	[SerializeField]
	private PrestigeTrophyViewer trophyViewer;

	[SerializeField]
	private Image medal;

	[SerializeField]
	private Image npcIcon;

	protected override void __Init()
	{
		base.__Init();
		trophyViewer.Init();
	}

	public void Render(TreatyPortFactionData data)
	{
		title.text = data.factionTitle;
		principal.text = data.principal;
		time.text = data.time;
		progress.text = data.progress;
		trophyViewer.Render(data.maxReputationLv, data.reputationLv);
		medal.sprite = data.medal;
		medal.gameObject.SetActive(data.medal != null && data.isComplete);
		npcIcon.sprite = data.npcIcon;
		bgImage.color = data.themeColor;
	}
}
