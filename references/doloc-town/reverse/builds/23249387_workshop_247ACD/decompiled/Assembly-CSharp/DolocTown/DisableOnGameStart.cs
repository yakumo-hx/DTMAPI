using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public class DisableOnGameStart : MonoBehaviour
{
	[SerializeField]
	private bool isClear;

	private void Awake()
	{
		if (isClear)
		{
			GetComponent<Tilemap>().color = Color.clear;
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
