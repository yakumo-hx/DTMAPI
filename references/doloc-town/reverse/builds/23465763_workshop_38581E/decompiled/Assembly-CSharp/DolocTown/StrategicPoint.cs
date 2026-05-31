using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class StrategicPoint : DolocObject, IStrategicPoint
{
	[SerializeField]
	private float radius;

	[SerializeField]
	private Vector3 offset;

	[SerializeField]
	private string monsterId;

	public string MonsterId => monsterId;

	public List<Transform> LockedTransforms { get; private set; } = new List<Transform>();


	public Vector2 Position => base.transform.position + offset;

	public float Radius => radius;

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			Transform item = other.gameObject.transform;
			if (!LockedTransforms.Contains(item))
			{
				LockedTransforms.Add(item);
			}
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		Transform item = other.gameObject.transform;
		if (LockedTransforms.Contains(item))
		{
			LockedTransforms.Remove(item);
		}
	}
}
