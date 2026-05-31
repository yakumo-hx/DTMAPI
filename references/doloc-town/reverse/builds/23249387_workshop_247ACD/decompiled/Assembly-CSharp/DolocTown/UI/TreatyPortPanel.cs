using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TreatyPortPanel : DolocUIPanel
{
	[SerializeField]
	private Text title;

	[SerializeField]
	public Text factionStats;

	[SerializeField]
	public TreatyPortFactionViewer dolocFactionViewer;

	[SerializeField]
	public FactionListViewer factionListViewer;

	[SerializeField]
	public Text settledHint;

	protected override void __Init()
	{
		base.__Init();
		dolocFactionViewer.Init();
	}

	protected override void OnStartShow()
	{
		title.text = base.staticTexts.TreatyPortTitle;
		settledHint.text = base.staticTexts.TreatyPortFactionSettledTip;
	}
}
