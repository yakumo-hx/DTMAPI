using UnityEngine;

namespace DolocTown;

public class MotorRenderer : DolocObject
{
	[SerializeField]
	private ParticleSystem psEngineFog;

	[SerializeField]
	private LayerMask groundMask;

	[SerializeField]
	private float probeLength;

	public bool ShouldRun { get; set; }

	private void Play()
	{
		if (!psEngineFog.gameObject.activeSelf)
		{
			psEngineFog.gameObject.SetActive(value: true);
		}
		if (!psEngineFog.isPlaying)
		{
			psEngineFog.Play();
		}
	}

	private void Stop()
	{
		if (psEngineFog.isPlaying)
		{
			psEngineFog.Stop();
		}
	}

	private void Update()
	{
		Vector2 vector = base.transform.position;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(vector, Vector2.down, probeLength, groundMask);
		if (raycastHit2D.collider != null)
		{
			Play();
			psEngineFog.transform.position = raycastHit2D.point;
		}
		else if (ShouldRun)
		{
			psEngineFog.transform.position = vector + Vector2.down;
			Play();
		}
		else
		{
			Stop();
		}
	}
}
