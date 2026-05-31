using RedSaw;
using UnityEngine;

namespace DolocTown;

public class Landmine : Skill
{
	[SerializeField]
	private Sprite dropSprite;

	[SerializeField]
	private Sprite normalSprite;

	[SerializeField]
	public Sprite[] counterSprites;

	[SerializeField]
	private SpriteRenderer counterSr;

	[SerializeField]
	private SimpleTrigger trigger;

	private readonly RSTimer timer = new RSTimer(0.3f);

	private float _lifeTimeCounter;

	private bool hasTouchGround;

	private bool hasTriggered;

	private int index;

	private float _criticalRate;

	public float Damage { get; set; }

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

	public float LiveDuration { get; set; } = 10f;


	public float ReflectionTime
	{
		get
		{
			return timer.currentInterval;
		}
		set
		{
			timer.SetInterval(value * 0.333333f);
		}
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		Damage = 0f;
		_criticalRate = 0f;
	}

	public void Throw(Vector2 force)
	{
		base.RB.AddForce(force, ForceMode2D.Impulse);
	}

	public override bool OnStart()
	{
		if (counterSprites == null || counterSprites.Length == 0)
		{
			return false;
		}
		if (counterSr == null || trigger == null)
		{
			return false;
		}
		SetVisible(value: true);
		timer.Reset();
		index = 0;
		_lifeTimeCounter = 0f;
		hasTouchGround = false;
		hasTriggered = false;
		trigger.OnTriggerEnter = OnTrigger;
		counterSr.gameObject.SetActive(value: false);
		trigger.gameObject.SetActive(value: false);
		base.spriteRenderer.color = Color.white;
		base.spriteRenderer.sprite = dropSprite;
		return true;
	}

	public override bool OnFixedUpdate(float dt)
	{
		if (hasTouchGround)
		{
			if (hasTriggered)
			{
				return Wait(dt);
			}
			if (_lifeTimeCounter >= LiveDuration)
			{
				TriggerLandmine();
			}
			_lifeTimeCounter += dt;
			return false;
		}
		Drop();
		return false;
	}

	private void OnTrigger(GameObject obj)
	{
		if (!hasTriggered && obj.CompareTag("Player"))
		{
			TriggerLandmine();
		}
	}

	private void TriggerLandmine()
	{
		counterSr.gameObject.SetActive(value: true);
		counterSr.sprite = counterSprites[index];
		hasTriggered = true;
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
			DolocAPI.EntitySystem.Next<Boom>().Invoke(base.transform.position, Damage, CriticalRate);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_DRONE_BOMB_COUNT_DOWN, base.gameObject);
			return true;
		}
		counterSr.sprite = counterSprites[index];
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN, base.gameObject);
		return false;
	}

	private void Drop()
	{
		if (!(Physics2D.Raycast(position, Vector2.down, 0.1f, DolocAPI.gameConfig.walkableMask).collider == null))
		{
			index = 0;
			hasTouchGround = true;
			base.RB.velocity = Vector2.zero;
			trigger.gameObject.SetActive(value: true);
			base.spriteRenderer.sprite = normalSprite;
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_DRONE_ATTACK_THROW_BOMB, base.gameObject);
		}
	}
}
