using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TechPointPreviewBox : DolocUiEntity
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text description;

	[SerializeField]
	private Text progress;

	protected override void __Init()
	{
		base.__Init();
		setInvisible();
	}

	public void Render(TechPointData data)
	{
		title.text = data.title;
		description.text = data.description;
		progress.text = data.progressInfo;
	}
}
