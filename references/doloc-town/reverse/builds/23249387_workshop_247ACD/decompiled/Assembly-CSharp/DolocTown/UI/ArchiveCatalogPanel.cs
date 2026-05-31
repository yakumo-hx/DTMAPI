using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ArchiveCatalogPanel : DolocUIPanel, IScrollContentRect
{
	[SerializeField]
	public CategoryOption optionItem;

	[SerializeField]
	public ArchiveListViewer archiveListViewer;

	[SerializeField]
	public ArchiveViewer archiveViewer;

	public ScrollRect scrollRect => archiveViewer.rect;

	public float moveDelta => 0.05f;

	protected override void __Init()
	{
		base.__Init();
		optionItem.Init();
	}

	public void OnMove()
	{
	}
}
