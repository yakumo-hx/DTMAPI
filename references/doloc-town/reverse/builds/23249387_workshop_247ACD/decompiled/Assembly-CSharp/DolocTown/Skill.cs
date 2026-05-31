using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class Skill : DolocRecyclableObject
{
	private Vector2 velocityCache;

	public SpriteRenderer spriteRenderer { get; private set; }

	public Animator animator { get; private set; }

	public Rigidbody2D RB { get; private set; }

	public Collider2D Collider { get; private set; }

	public bool ShouldWorkOnMonster { get; set; }

	protected override void __Init()
	{
		base.__Init();
		spriteRenderer = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
		RB = GetComponent<Rigidbody2D>();
		Collider = GetComponent<Collider2D>();
	}

	public abstract bool OnStart();

	public abstract bool OnFixedUpdate(float dt);

	public override void OnReuse()
	{
		base.OnReuse();
		DolocAPI.Sound.RegisterGameObject(base.gameObject);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_MONSTER_BUS, base.gameObject);
		DolocAPI.Sound.UnregisterGameObject(base.gameObject);
	}

	public virtual void OnGamePaused()
	{
		velocityCache = RB.velocity;
		RB.velocity = Vector2.zero;
		RB.isKinematic = true;
	}

	public virtual void OnGameResumed()
	{
		RB.isKinematic = false;
		RB.velocity = velocityCache;
	}
}
