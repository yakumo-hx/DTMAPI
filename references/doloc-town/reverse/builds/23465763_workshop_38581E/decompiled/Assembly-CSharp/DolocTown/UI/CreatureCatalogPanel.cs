using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CreatureCatalogPanel : DolocUIPanel, IScrollContentRect
{
	[SerializeField]
	public CreatureListViewer creatureListViewer;

	[SerializeField]
	public CreatureViewer creatureViewer;

	public ScrollRect scrollRect => creatureViewer.rect;

	public float moveDelta => 0.05f;

	public void OnMove()
	{
	}
}
