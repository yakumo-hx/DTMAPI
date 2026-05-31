using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CropInfoBox : SceneBoxBase
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text growthLevel;

	[SerializeField]
	private Text harvestCount;

	[SerializeField]
	private Text geneInfo;

	[SerializeField]
	private GameObject lightIcon;

	[SerializeField]
	private ProgressBar growthBar;

	[SerializeField]
	private ProgressBar healthBar;

	[SerializeField]
	private ProgressBar waterBar;

	[SerializeField]
	private ProgressBar filmBar;

	[SerializeField]
	private ProgressBar fertilizerBar;

	protected override void __Init()
	{
		base.__Init();
		ProgressBar[] componentsInChildren = GetComponentsInChildren<ProgressBar>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Init();
		}
	}

	public void Render(CropInfoData data)
	{
		lightIcon.SetActive(data.isLighting);
		SetText(title, data.title);
		SetText(growthLevel, data.growthLevelInfo);
		SetText(harvestCount, data.harvestInfo);
		SetText(geneInfo, data.geneInfo);
		SetProgressBar(growthBar, data.growthProgress);
		SetProgressBar(healthBar, data.healthProgress);
		SetProgressBar(waterBar, data.waterProgress, setGray: true);
		SetProgressBar(filmBar, data.filmProgress, setGray: true);
		SetProgressBar(fertilizerBar, data.fertilizerProgress, setGray: true);
		DolocAPI.DelayFrame(base.RebuildLayout);
	}

	private void SetProgressBar(ProgressBar progressBar, float value, bool setGray = false)
	{
		progressBar.SetVisible(value >= 0f);
		progressBar.grayed = setGray && value == 0f;
		progressBar.SetProgress(value);
	}
}
