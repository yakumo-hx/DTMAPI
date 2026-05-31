using RedSaw;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnvironmentChecker : MonoBehaviour
{
	[SerializeField]
	private LayerMask layerMask;

	private int counter;

	private bool flag;

	private Collider2D collider2d;

	public bool isTouched => flag;

	private void Start()
	{
		collider2d = GetComponent<Collider2D>();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (RSUtils.LayerMaskCheck(other.gameObject, layerMask))
		{
			counter++;
			flag = counter > 0;
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (RSUtils.LayerMaskCheck(other.gameObject, layerMask))
		{
			counter--;
			flag = counter > 0;
		}
	}

	private void OnDrawGizmos()
	{
		if (collider2d != null)
		{
			GizmosHelper.DrawColliderBox(collider2d, flag ? Color.red : Color.cyan);
		}
	}
}
