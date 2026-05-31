using System;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class SimpleTrigger : MonoBehaviour
{
	public Action<GameObject> OnTriggerEnter { get; set; }

	private void OnTriggerEnter2D(Collider2D other)
	{
		OnTriggerEnter?.Invoke(other.gameObject);
	}
}
