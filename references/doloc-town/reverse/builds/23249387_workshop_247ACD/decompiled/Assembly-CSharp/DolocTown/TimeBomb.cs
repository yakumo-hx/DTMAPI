using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
public class TimeBomb : Skill
{
	[SerializeField]
	private Sprite dropSprite;

	[SerializeField]
	private Sprite normalSprite;

	[SerializeField]
	public Sprite[] counterSprites;

	[SerializeField]
	private SpriteRenderer counterSr;

	private readonly RSTimer timer = new RSTimer();

	private readonly RSTimer insuranceTimer = new RSTimer(5f);

	private bool hasTouchGround;

	private int index;

	private float _criticalRate;

	private bool insuranceLatch;

	private bool hasBoomed;

	public float Damage { get; set; }

	public bool IsItemBomb { get; set; }

	public float CriticalRate
	{
		get
		{
			return _criticalRate;
		}
		set
		{
			_criticalRate = Mathf.Clamp01(value);
		}
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		if (IsItemBomb && !hasBoomed)
		{
			Item data = DolocAPI.GenerateItem(DolocAPI.GlobalParameter.ItemRefTimeBomb);
			((IDropItemHost)DolocAPI.CurrentRoom).CreateDropItem(data, position2d, shouldSendMsg: false, 0f);
		}
		Damage = 0f;
		_criticalRate = 0f;
		base.ShouldWorkOnMonster = false;
		IsItemBomb = false;
		hasBoomed = false;
	}

	public override bool OnStart()
	{
		if (counterSprites == null || counterSprites.Length == 0)
		{
			DolocAPI.outputError("计时器贴图为空");
			return false;
		}
		if (counterSr == null)
		{
			DolocAPI.outputError("计时器渲染器为空");
			return false;
		}
		SetVisible(value: true);
		timer.Reset();
		insuranceTimer.Reset();
		insuranceLatch = false;
		index = 0;
		hasBoomed = false;
		hasTouchGround = false;
		counterSr.gameObject.SetActive(value: false);
		base.spriteRenderer.color = Color.white;
		base.spriteRenderer.sprite = dropSprite;
		Rigidbody2D component = GetComponent<Rigidbody2D>();
		if (component != null)
		{
			component.bodyType = RigidbodyType2D.Dynamic;
		}
		return true;
	}

	public override bool OnFixedUpdate(float dt)
	{
		if (hasTouchGround)
		{
			return Wait(dt);
		}
		if (insuranceLatch)
		{
			return true;
		}
		if (insuranceTimer.Tick(dt))
		{
			insuranceLatch = true;
		}
		return false;
	}

	private bool Wait(float dt)
	{
		base.RB.velocity = new Vector2(0f, base.RB.velocity.y);
		if (!timer.Tick(dt))
		{
			return false;
		}
		if (++index >= counterSprites.Length)
		{
			hasBoomed = true;
			DolocAPI.EntitySystem.Next<Boom>().Invoke(base.transform.position, Damage, CriticalRate, shouldRaiseScreenTwist: true, base.ShouldWorkOnMonster);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_DRONE_BOMB_COUNT_DOWN, base.gameObject);
			return true;
		}
		counterSr.sprite = counterSprites[index];
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN, base.gameObject);
		return false;
	}

	private void OnLandGround()
	{
		index = 0;
		hasTouchGround = true;
		base.RB.velocity = Vector2.zero;
		counterSr.gameObject.SetActive(value: true);
		counterSr.sprite = counterSprites[index];
		base.spriteRenderer.sprite = normalSprite;
		DolocAPI.RaiseInstantAnimEffects(position2d, InstAnimEffectType.PLAYER_LAND_SMOKE);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_DRONE_ATTACK_THROW_BOMB, base.gameObject);
	}

	private void OnCollisionEnter2D(Collision2D other)
	{
		if (!hasTouchGround && RSUtils.LayerMaskCheck(other.gameObject, DolocAPI.gameConfig.walkableMask))
		{
			OnLandGround();
		}
	}
}
