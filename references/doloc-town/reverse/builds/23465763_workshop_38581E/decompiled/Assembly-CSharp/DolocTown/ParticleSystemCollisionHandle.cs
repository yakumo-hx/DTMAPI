using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSystemCollisionHandle : MonoBehaviour
{
	private readonly List<ParticleCollisionEvent> events = new List<ParticleCollisionEvent>();

	private ParticleSystem _ps;

	public ParticleSystem ps
	{
		get
		{
			if (_ps == null)
			{
				_ps = GetComponent<ParticleSystem>();
			}
			return _ps;
		}
	}

	protected virtual void OnParticleCollisionEnter(ParticleCollisionEvent evt, GameObject other)
	{
	}

	private void OnParticleCollision(GameObject other)
	{
		if (!base.enabled || ps.GetCollisionEvents(other, events) == 0)
		{
			return;
		}
		foreach (ParticleCollisionEvent @event in events)
		{
			OnParticleCollisionEnter(@event, other);
		}
	}
}
