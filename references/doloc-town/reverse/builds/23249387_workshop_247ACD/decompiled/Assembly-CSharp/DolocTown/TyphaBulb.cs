using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(PolygonCollider2D), typeof(Animator))]
public class TyphaBulb : LuminousPlant, IInteractable
{
	public Vector2 effectsOffset;

	public GameObject idleEffect;

	private Collider2D collider;

	private Animator animator;

	private bool isTouched;

	public bool OnlyTouch => true;

	public bool CanInteractContinues => false;

	public override void Init()
	{
		base.Init();
		collider = GetComponent<Collider2D>();
		animator = GetComponent<Animator>();
		animator.enabled = false;
		animator.runtimeAnimatorController = DolocAPI.GetAsset<RuntimeAnimatorController>(DolocGameAssets.GAME_ANIM_UNIVERSAL_PLANT);
	}

	public void PlayAnimation()
	{
		if (!animator.enabled && base.gameObject.activeSelf)
		{
			animator.enabled = true;
			DolocAPI.SwingPlantOnTouch(animator, light: true);
		}
	}

	public void StopAnimation()
	{
		animator.enabled = false;
	}

	protected override void TurnOff(bool isInitial)
	{
		base.TurnOff(isInitial);
		idleEffect.SetActive(value: false);
		StopAnimation();
	}

	protected override void TurnOn(bool isInitial)
	{
		base.TurnOn(isInitial);
		idleEffect.SetActive(value: true);
	}

	public void OnTouch()
	{
		isTouched = true;
		PlayAnimation();
		DolocAPI.RaiseInstantPSEffects((Vector2)base.transform.position + effectsOffset, InstantParticleEffectsType.GLOWING_PARTICLES);
	}

	public void OnDisTouch()
	{
		isTouched = false;
		PlayAnimation();
	}

	public void OnInteract()
	{
	}

	private void OnDrawGizmos()
	{
		if (collider != null)
		{
			GizmosHelper.DrawColliderBox(collider, isTouched ? Color.red : Color.cyan);
		}
	}
}
