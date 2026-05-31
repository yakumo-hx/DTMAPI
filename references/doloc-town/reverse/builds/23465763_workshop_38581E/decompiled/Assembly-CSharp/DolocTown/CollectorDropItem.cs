using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(CircleCollider2D))]
public class CollectorDropItem : MonoBehaviour
{
	[SerializeField]
	private float maxMoveSpeed = 1f;

	[SerializeField]
	private float minMoveSpeed = 0.5f;

	[SerializeField]
	private float pickDistance = 0.5f;

	private readonly List<DropItemRenderer> dropitems = new List<DropItemRenderer>();

	private float radiusReciprocal;

	public void Init()
	{
		CircleCollider2D component = GetComponent<CircleCollider2D>();
		component.isTrigger = true;
		radiusReciprocal = 1f / component.radius;
	}

	public void ResetRadius(float radius)
	{
		if (!(radius <= 0f))
		{
			GetComponent<CircleCollider2D>().radius = radius;
			radiusReciprocal = 1f / radius;
		}
	}

	public void OnFixedUpdate(float dt)
	{
		if (dropitems.Count == 0)
		{
			return;
		}
		for (int i = 0; i < dropitems.Count; i++)
		{
			DropItemRenderer dropItemRenderer = dropitems[i];
			if (IsCollected(dropItemRenderer))
			{
				dropitems.RemoveAt(i);
				i--;
			}
			else
			{
				_Collect(dropItemRenderer, dt);
			}
		}
	}

	private void _Collect(DropItemRenderer obj, float dt)
	{
		Vector2 vector = base.transform.position - obj.transform.position;
		float magnitude = vector.magnitude;
		if (magnitude < pickDistance)
		{
			obj.SetShieldCollector(shield: true);
			return;
		}
		Vector2 vector2 = vector.normalized * dt;
		obj.transform.Translate(vector2 * Mathf.Lerp(minMoveSpeed, maxMoveSpeed, magnitude * radiusReciprocal));
	}

	private static bool IsCollected(DropItemRenderer dropitem)
	{
		if (!(dropitem == null) && dropitem.isVisible)
		{
			return dropitem.shieldCollector;
		}
		return true;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		DropItemRenderer component = other.GetComponent<DropItemRenderer>();
		if (!(component == null) && !dropitems.Contains(component) && !component.shieldCollector)
		{
			dropitems.Add(component);
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		DropItemRenderer component = other.GetComponent<DropItemRenderer>();
		if (!(component == null) && dropitems.Contains(component))
		{
			dropitems.Remove(component);
		}
	}
}
