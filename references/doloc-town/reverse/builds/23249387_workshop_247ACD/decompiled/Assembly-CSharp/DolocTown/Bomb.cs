using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bomb : Skill
{
	[SerializeField]
	private bool isEmissive;

	[SerializeField]
	private Sprite emissionMask;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color emissionColor = Color.white;

	[SerializeField]
	[Min(0f)]
	[Tooltip("超出时间如果炸弹仍未碰到地面则会直接回收")]
	private float lifeTime = 10f;

	private float _lifeTimeCounter;

	private bool _shouldQuit;

	private float _criticalRate;

	public Rigidbody2D _rigidbody2D { get; protected set; }

	public Collider2D _collider2D { get; protected set; }

	public Vector2 Velocity
	{
		get
		{
			return _rigidbody2D.velocity;
		}
		set
		{
			_rigidbody2D.velocity = value;
		}
	}

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

	protected override void __Init()
	{
		base.__Init();
		_rigidbody2D = GetComponent<Rigidbody2D>();
		_collider2D = GetComponent<Collider2D>();
	}

	public override void OnCreated()
	{
		base.OnCreated();
		HandleEmission();
	}

	public override void OnReuse()
	{
		base.OnReuse();
		HandleEmission();
	}

	private void HandleEmission()
	{
		if (isEmissive && !(emissionMask == null))
		{
			SpriteRenderer component = GetComponent<SpriteRenderer>();
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			materialPropertyBlock.SetTexture("_EmissionTex", emissionMask.texture);
			materialPropertyBlock.SetColor("_EmissionColor", emissionColor);
			component.SetPropertyBlock(materialPropertyBlock);
		}
	}

	public void AddForce(Vector2 force)
	{
		_rigidbody2D.AddForce(force, ForceMode2D.Impulse);
	}

	public override bool OnStart()
	{
		_lifeTimeCounter = 0f;
		_shouldQuit = false;
		return true;
	}

	public override bool OnFixedUpdate(float dt)
	{
		if (_lifeTimeCounter >= lifeTime)
		{
			return true;
		}
		_lifeTimeCounter += dt;
		return _shouldQuit;
	}

	public void OnCollisionEnter2D(Collision2D other)
	{
		if (base.ShouldWorkOnMonster)
		{
			if (RSUtils.LayerMaskCheck(other.gameObject, (int)DolocAPI.gameConfig.walkableMask | (int)DolocAPI.gameConfig.enemyMask))
			{
				_shouldQuit = true;
				DolocAPI.EntitySystem.Next<Boom>().Invoke(base.transform.position, Damage, CriticalRate, shouldRaiseScreenTwist: true, shouldWorkOnMonster: true);
			}
		}
		else if (RSUtils.LayerMaskCheck(other.gameObject, (int)DolocAPI.gameConfig.walkableMask | (int)DolocAPI.gameConfig.agentMask))
		{
			_shouldQuit = true;
			DolocAPI.EntitySystem.Next<Boom>().Invoke(base.transform.position, Damage, CriticalRate);
		}
	}
}
