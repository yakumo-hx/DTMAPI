using UnityEngine;

namespace DolocTown;

public class MonsterMoverRendererChomper : MonsterMoverShuttleRenderer
{
	private MonsterController _controller;

	private MonsterController Controller
	{
		get
		{
			if (_controller == null)
			{
				_controller = GetComponent<MonsterController>();
			}
			return _controller;
		}
	}

	private Vector2 EffectsPosition => base.transform.position + new Vector3(0f, 0.75f);

	public override void OnShuttleIn()
	{
		DolocAPI.RaiseInstantPSEffects(EffectsPosition, InstantParticleEffectsType.LEAVES_AND_SOILS);
		Controller.Renderer.PlayAnimation("drill_in", 0, 0f, delegate
		{
			base.transform.GetComponent<Collider2D>().enabled = false;
		});
	}

	public override void OnShuttleOut()
	{
		DolocAPI.RaiseInstantPSEffects(EffectsPosition, InstantParticleEffectsType.LEAVES_AND_SOILS);
		base.transform.GetComponent<Collider2D>().enabled = true;
		Controller.Renderer.PlayAnimation("drill_out");
	}
}
