using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown.UI;

public class UiGroup : MonoBehaviour
{
	private List<DolocUiEntity> entities = new List<DolocUiEntity>();

	public RectTransform container { get; private set; }

	public void Init()
	{
		container = base.transform.Find("Container").GetComponent<RectTransform>();
	}

	public void AddEntity(DolocUiEntity entity)
	{
		entities.Add(entity);
		SortEntities();
	}

	private void SortEntities()
	{
		entities = entities.OrderBy((DolocUiEntity x) => x.SortOrderInGroup).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].transform.SetSiblingIndex(i);
		}
	}

	public void RemoveEntity(DolocUiEntity entity)
	{
		if (entities.Remove(entity))
		{
			Object.Destroy(entity.gameObject);
		}
	}
}
