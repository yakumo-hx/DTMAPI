using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class NpcCatalogPanel : DolocUIPanel, IScrollContentRect
{
	[SerializeField]
	public NpcListViewer npcListViewer;

	[SerializeField]
	public NpcViewer npcViewer;

	public ScrollRect scrollRect => npcViewer.rect;

	public float moveDelta => 0.05f;

	public void OnMove()
	{
	}
}
