using UnityEngine;

namespace DolocTown;

public class ChomperMimicry : Skill, IFellable
{
	private float _currentDuration;

	public float Duration
	{
		set
		{
			_currentDuration = value;
		}
	}

	public bool ShouldCostEnergy => false;

	public bool ShouldCostChopCounter => false;

	public override bool OnStart()
	{
		Rigidbody2D component = GetComponent<Rigidbody2D>();
		component.gravityScale = 0f;
		component.bodyType = RigidbodyType2D.Static;
		return true;
	}

	public override bool OnFixedUpdate(float dt)
	{
		_currentDuration -= dt;
		if (_currentDuration <= 0f)
		{
			DolocAPI.RaiseInstantPSEffects(GetComponent<Collider2D>().bounds.center, InstantParticleEffectsType.BUNCH_OF_LEAVES);
			return true;
		}
		return false;
	}

	public bool OnFell(ItemTool tool, Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS_BARK);
		_currentDuration = 0f;
		return true;
	}

	public override void OnGamePaused()
	{
	}

	public override void OnGameResumed()
	{
	}
}
