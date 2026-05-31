using RedSaw;
using UnityEngine;

public class OneWayPlatformOnSlope : MonoBehaviour
{
	[SerializeField]
	private Vector4 bounds1;

	[SerializeField]
	private Vector4 bounds2;

	private bool hasTouched;

	private BoxCollider2D collider2d;

	private EdgeCollider2D platformCollider;

	private void Start()
	{
		collider2d = GetComponent<BoxCollider2D>();
		platformCollider = GetComponentInParent<EdgeCollider2D>();
		Debug.Log(platformCollider == collider2d);
		base.enabled = false;
	}

	private void setBounds(Vector4 bounds)
	{
		collider2d.offset = new Vector2(bounds.x, bounds.y);
		collider2d.size = new Vector2(bounds.z, bounds.w);
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.S))
		{
			platformCollider.enabled = false;
			setBounds(bounds1);
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			hasTouched = true;
			base.enabled = true;
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			hasTouched = false;
			platformCollider.enabled = true;
			base.enabled = false;
			setBounds(bounds2);
		}
	}

	private void OnDrawGizmos()
	{
		if (collider2d != null)
		{
			GizmosHelper.DrawColliderBox(collider2d, hasTouched ? Color.yellow : Color.red);
		}
	}
}
