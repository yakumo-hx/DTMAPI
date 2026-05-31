using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AnimalInfoBox : SceneBoxBase
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text year;

	[SerializeField]
	private Text state;

	[SerializeField]
	private ProgressBar energyBar;

	[SerializeField]
	private ProgressBar moodBar;

	public void Render(AnimalBaseInfoData data)
	{
		SetText(title, data.title);
		SetText(year, data.ageInfo);
		SetText(state, data.stateBaseInfo);
		energyBar.SetProgress(data.energyProgress);
		moodBar.SetProgress(data.moodProgress);
		RebuildLayout();
	}
}
