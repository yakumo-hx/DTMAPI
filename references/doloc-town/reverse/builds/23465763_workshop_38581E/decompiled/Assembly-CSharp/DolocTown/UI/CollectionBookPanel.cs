using UnityEngine;

namespace DolocTown.UI;

public class CollectionBookPanel : DolocUIPanel
{
	[SerializeField]
	public CollectionBookSubMenu subMenu;

	[SerializeField]
	public ItemCatalogPanel itemCatalogPanel;

	[SerializeField]
	public CreatureCatalogPanel creatureCatalogPanel;

	[SerializeField]
	public MonsterCatalogPanel monsterCatalogPanel;

	[SerializeField]
	public ResourceCatalogPanel resourceCatalogPanel;

	[SerializeField]
	public NpcCatalogPanel npcCatalogPanel;

	[SerializeField]
	public ArchiveCatalogPanel archiveCatalogPanel;

	[SerializeField]
	public MapPanel mapPanel;
}
