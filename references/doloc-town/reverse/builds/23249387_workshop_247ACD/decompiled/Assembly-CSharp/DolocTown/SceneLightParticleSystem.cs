using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(ParticleSystem))]
public class SceneLightParticleSystem : SceneLight
{
	private ParticleSystem ps;

	public override void Init()
	{
		base.Init();
		ps = GetComponent<ParticleSystem>();
	}

	protected override void TurnOff(bool isInitial)
	{
		if (ps == null)
		{
			Init();
		}
		ps.Stop();
		if (isInitial)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	protected override void TurnOn(bool isInitial)
	{
		if (ps == null)
		{
			Init();
		}
		base.gameObject.SetActive(value: true);
		ps.Play();
	}
}
