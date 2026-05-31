using UnityEngine;

namespace DolocTown;

public class GameEntityManagerInfo : MonoBehaviour
{
	[HideInInspector]
	public GameEntityManager gameEntityManager;

	public GameObject Entity => gameEntityManager.Prefab;

	public int TotalCount => gameEntityManager.ActivedGameEntityCount + gameEntityManager.CacheCount;

	public int ActiveCount => gameEntityManager.ActivedGameEntityCount;

	public int CacheCount => gameEntityManager.CacheCount;

	public string RecycleInfo => gameEntityManager.RecycleCounterInfo;

	public string Alias => gameEntityManager.alias;

	public bool CustomManaged => gameEntityManager.customManaged;
}
