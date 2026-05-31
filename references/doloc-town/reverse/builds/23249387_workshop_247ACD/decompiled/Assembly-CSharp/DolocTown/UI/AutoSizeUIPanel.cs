using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(ContentSizeFitter))]
public abstract class AutoSizeUIPanel : DolocUIPanel
{
	protected ContentSizeFitter panelContentSizeFitter;

	protected LayoutGroup panelLayoutGroup;

	protected override void __Init()
	{
		base.__Init();
		panelContentSizeFitter = GetComponent<ContentSizeFitter>();
		if (!TryGetComponent<LayoutGroup>(out panelLayoutGroup))
		{
			panelLayoutGroup = base.gameObject.AddComponent<VerticalLayoutGroup>();
			VerticalLayoutGroup obj = panelLayoutGroup as VerticalLayoutGroup;
			obj.childControlHeight = true;
			obj.childControlWidth = true;
			obj.childForceExpandHeight = false;
			obj.childForceExpandWidth = false;
		}
		panelContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		panelContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		RebuildLayout();
	}

	public new void MoveToCenter(CenterType type)
	{
		RebuildLayout();
		base.MoveToCenter(type);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		RebuildLayout();
	}
}
