using UnityEngine;

namespace DolocTown;

[GameEntityManager("/dungeon/env_objects", DolocGameAssets.GAME_ENTITY_WORLD_CONTENT_ENV)]
public class EnvObjectRenderer : WorldContentRenderer
{
	[SerializeField]
	private GameObject entity;

	private Animator _animator;

	private bool shouldUpdate;

	private Vector2 speed;

	private float totalTime;

	public RuntimeAnimatorController AnimatorController
	{
		get
		{
			return _animator.runtimeAnimatorController;
		}
		set
		{
			_animator.runtimeAnimatorController = value;
		}
	}

	public override bool OnlyTouch => true;

	protected override void __Init()
	{
		Sr = entity.GetComponent<SpriteRenderer>();
		_animator = entity.GetComponent<Animator>();
		Col2d = GetComponent<CircleCollider2D>();
	}

	private void Update()
	{
		if (shouldUpdate)
		{
			if (totalTime > 0f)
			{
				totalTime -= Time.deltaTime;
				base.transform.Translate(speed * Time.deltaTime, Space.World);
			}
			else
			{
				shouldUpdate = false;
				DolocAPI.EntitySystem.Recycle(this);
			}
		}
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		shouldUpdate = false;
		_animator.enabled = false;
	}

	public void SetMoveTarget(Vector2 target, float speed)
	{
		Sr.flipX = target.x < 0f;
		shouldUpdate = true;
		float num = Vector2.Distance(target, Vector2.zero);
		totalTime = num / speed;
		this.speed = target.normalized * speed;
	}

	public void SetColRadius(float radius)
	{
		Col2d.radius = radius;
	}

	public void SetSrFlipX(bool value)
	{
		Sr.flipX = value;
	}

	public void PlayAnimation(string name)
	{
		if (!(AnimatorController == null) && base.gameObject.activeSelf)
		{
			_animator.enabled = true;
			_animator.Play(name, 0, 0f);
		}
	}

	protected virtual void StopAnimation()
	{
		_animator.enabled = false;
	}
}
