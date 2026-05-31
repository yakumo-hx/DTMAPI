using UnityEngine;

namespace DolocTown;

public class WindDebugger : MonoBehaviour
{
	[SerializeField]
	private float force = 100f;

	[SerializeField]
	private Vector2 direction = Vector2.right;

	[SerializeField]
	private float duration = 3f;

	private void CreateWind()
	{
		DolocAPI.RaiseWind(base.transform.position, direction.normalized * force, duration);
	}
}
