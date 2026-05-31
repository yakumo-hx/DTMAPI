using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Animator))]
[GameEffectsManager("/continues_anims", DolocGameAssets.GAME_ENTITY_CONTINUES_ANIM)]
public class ContinuesAnimationEffects : GameEntity
{
	private SpriteRenderer _spriteRenderer;

	private Animator _animator;

	public Material material
	{
		get
		{
			return _spriteRenderer.sharedMaterial;
		}
		set
		{
			_spriteRenderer.sharedMaterial = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		_animator = GetComponent<Animator>();
		_spriteRenderer = GetComponent<SpriteRenderer>();
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		_spriteRenderer.sharedMaterial = LocMaterials.GAME_MAT_2D;
		_spriteRenderer.sortingLayerName = "Default";
		_spriteRenderer.sortingOrder = 0;
		_spriteRenderer.color = Color.white;
	}

	public void Play(string name, float normalizedTime = 0f)
	{
		_animator.Play(name, 0, normalizedTime);
	}
}
