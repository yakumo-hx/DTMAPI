using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public abstract class InteractableObjectExclude : InteractableObject, IInteractableExclude, IInteractable
{
	public Collision2D currentCollision { get; private set; }

	public Collider2D currentOtherCollider { get; private set; }

	protected abstract ITouchCheckStrategy touchChecker { get; set; }

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!base.isTouched && touchChecker.Check(other.gameObject))
		{
			base.isTouched = true;
			currentOtherCollider = other;
			OnTouch();
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (base.isTouched && touchChecker.Check(other.gameObject))
		{
			OnDisTouch();
			base.isTouched = false;
			currentOtherCollider = null;
		}
	}

	private void OnCollisionEnter2D(Collision2D other)
	{
		if (touchChecker.Check(other.gameObject))
		{
			currentCollision = other;
		}
	}

	private void OnCollisionExit2D(Collision2D other)
	{
		if (touchChecker.Check(other.gameObject))
		{
			currentCollision = null;
		}
	}
}
