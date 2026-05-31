using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MonsterCatalogPanel : DolocUIPanel, IScrollContentRect
{
	[SerializeField]
	public MonsterListViewer monsterListViewer;

	[SerializeField]
	public MonsterViewer monsterViewer;

	public ScrollRect scrollRect => monsterViewer.rect;

	public float moveDelta => 0.05f;

	public void OnMove()
	{
	}
}
