using UnityEngine;

public class BoxExample : MonoBehaviour
{
	public float m_MaxDistance;

	public float m_Speed;

	public bool m_HitDetect;

	private Collider2D m_Collider;

	private RaycastHit m_Hit;

	private void Start()
	{
		m_MaxDistance = 300f;
		m_Speed = 20f;
		m_Collider = GetComponent<Collider2D>();
	}

	private void FixedUpdate()
	{
		m_HitDetect = Physics.BoxCast(m_Collider.bounds.center, base.transform.localScale, base.transform.forward, out m_Hit, base.transform.rotation, m_MaxDistance);
		if (m_HitDetect)
		{
			Debug.Log("Hit : " + m_Hit.collider.name);
		}
	}

	private void OnDrawGizmos()
	{
		if (m_HitDetect)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawRay(base.transform.position, base.transform.forward * m_Hit.distance);
			Gizmos.DrawWireCube(base.transform.position + base.transform.forward * m_Hit.distance, base.transform.localScale);
			Gizmos.DrawSphere(m_Hit.point, 0.05f);
		}
		else
		{
			Gizmos.color = Color.red;
			Gizmos.DrawRay(base.transform.position, base.transform.forward * m_MaxDistance);
			Gizmos.DrawWireCube(base.transform.position + base.transform.forward * m_MaxDistance, base.transform.localScale);
		}
	}
}
