using UnityEngine;

namespace DolocTown;

public class AircraftEngineSmoke : MonoBehaviour
{
	[SerializeField]
	private LayerMask _collisionLayer;

	[SerializeField]
	private ParticleSystem _particleSystem;

	[SerializeField]
	[Min(0f)]
	private float _detectDistance = 10f;

	private void Update()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, Vector2.down, _detectDistance, _collisionLayer);
		if (raycastHit2D.collider != null)
		{
			_particleSystem.transform.position = raycastHit2D.point;
			if (!_particleSystem.isPlaying)
			{
				_particleSystem.Play();
			}
		}
		else if (_particleSystem.isPlaying)
		{
			_particleSystem.Stop();
		}
	}
}
