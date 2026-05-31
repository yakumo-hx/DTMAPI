using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class MonsterInteractableScanner : MonoBehaviour
{
	private void OnTriggerEnter2D(Collider2D other)
	{
		IMonsterInteractable component = other.GetComponent<IMonsterInteractable>();
		if (component != null)
		{
			Collider2D component2 = GetComponent<Collider2D>();
			component.OnMonsterTouch(other.ClosestPoint(component2.bounds.center));
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		IMonsterInteractable component = other.GetComponent<IMonsterInteractable>();
		if (component != null)
		{
			Collider2D component2 = GetComponent<Collider2D>();
			component.OnMonsterDisTouch(other.ClosestPoint(component2.bounds.center));
		}
	}
}
