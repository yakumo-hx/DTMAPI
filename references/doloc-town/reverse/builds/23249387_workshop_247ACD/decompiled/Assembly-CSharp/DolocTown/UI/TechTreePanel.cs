using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TechTreePanel : DolocUIPanel
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text treeDesc;

	[SerializeField]
	public TechTreeTitleMenu treeMenu;

	[SerializeField]
	public TechPointListViewer techPointViewer;

	[SerializeField]
	public TechPointPreviewBox pointPreviewBox;

	[SerializeField]
	public TechTreeWidget treeWidget;

	[SerializeField]
	public TechNodePreviewViewer previewViewer;

	public string TreeDesc
	{
		set
		{
			treeDesc.text = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		pointPreviewBox.Init();
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		title.text = base.staticTexts.TechtreePanelTitle;
	}
}
