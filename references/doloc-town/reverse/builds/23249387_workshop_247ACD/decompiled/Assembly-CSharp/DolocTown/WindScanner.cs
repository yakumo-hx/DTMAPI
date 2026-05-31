using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class WindScanner : GameEntity
{
	private void OnTriggerEnter2D(Collider2D other)
	{
		other.GetComponent<IWindInteractive>()?.OnWindBlow(base.transform.position);
	}
}
