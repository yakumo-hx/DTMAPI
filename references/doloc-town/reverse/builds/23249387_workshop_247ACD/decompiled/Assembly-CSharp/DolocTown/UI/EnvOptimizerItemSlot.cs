using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EnvOptimizerItemSlot : DolocNavigationButton
{
	[SerializeField]
	private Text title;

	[FormerlySerializedAs("progressBar")]
	[SerializeField]
	private ProgressBar progressBorder;

	private EnvOptimizerItemData data;

	public bool isEmpty => !data.notEmpty;

	protected override void __Init()
	{
		base.__Init();
		Clear();
		progressBorder.Init();
	}

	public void Render(EnvOptimizerItemData data)
	{
		this.data = data;
		if (!data.notEmpty)
		{
			Clear();
			return;
		}
		title.text = data.title;
		base.iconSprite = data.sprite;
		base.iconColor = Color.white;
		SetBorderProgress(data.isActive ? 1 : 0);
	}

	public void Clear()
	{
		title.text = "";
		base.iconColor = DolocColor.empty;
		SetBorderProgress(0f);
	}

	public void SetBorderProgress(float progress)
	{
		progressBorder.SetProgress(progress);
	}
}
