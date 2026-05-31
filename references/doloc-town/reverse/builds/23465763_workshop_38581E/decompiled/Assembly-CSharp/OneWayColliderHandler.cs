using UnityEngine;

public class OneWayColliderHandler : MonoBehaviour
{
	private PlatformEffector2D effector2D;

	private Collider2D collider2d;

	private float normalAnlge;

	private float abnormalAngle;

	public bool isEnable;

	private void Start()
	{
		effector2D = GetComponent<PlatformEffector2D>();
		collider2d = GetComponent<Collider2D>();
		normalAnlge = effector2D.rotationalOffset;
		abnormalAngle = normalAnlge + 180f;
	}

	public void SetEnabled(bool value)
	{
		effector2D.rotationalOffset = (value ? normalAnlge : abnormalAngle);
		collider2d.enabled = value;
		isEnable = value;
	}
}
