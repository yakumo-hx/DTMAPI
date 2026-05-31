using UnityEngine;

namespace DolocTown;

public class InWalkableArea : MonoBehaviour
{
	[SerializeField]
	[Min(0f)]
	private float width;

	[SerializeField]
	private bool hideGizmos;

	public Vector2 Proto
	{
		get
		{
			Vector3 position = base.transform.position;
			return new Vector2(position.x, position.x + width);
		}
	}
}
