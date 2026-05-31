using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class ColliderGroupPreset : DolocObject
{
	private void ResetColliders()
	{
		EdgeCollider2D[] componentsInChildren = GetComponentsInChildren<EdgeCollider2D>();
		foreach (EdgeCollider2D edgeCollider2D in componentsInChildren)
		{
			Vector2 vector = edgeCollider2D.points[0] * edgeCollider2D.transform.localScale;
			Vector2 vector2 = edgeCollider2D.points[^1] * edgeCollider2D.transform.localScale - vector;
			edgeCollider2D.transform.localScale = Vector3.one;
			edgeCollider2D.points = new Vector2[2]
			{
				Vector2.zero,
				vector2
			};
		}
	}

	public IEnumerable<OnewayColliderProto> GetProto()
	{
		ResetColliders();
		return from x in GetComponentsInChildren<EdgeCollider2D>()
			select new OnewayColliderProto(x.transform.localPosition, x.points[^1].x);
	}
}
