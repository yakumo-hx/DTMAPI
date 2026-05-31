using UnityEngine;

namespace RedSaw.Physical;

public class PhysicalObject2D : MonoBehaviour
{
	[SerializeField]
	private Vector2 force;

	private void ThrowUp()
	{
		GetComponent<Rigidbody2D>().AddForce(force, ForceMode2D.Impulse);
	}
}
