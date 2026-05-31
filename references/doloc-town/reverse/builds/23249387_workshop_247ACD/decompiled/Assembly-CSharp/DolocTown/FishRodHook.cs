using System;
using DG.Tweening;
using RedSaw.Physical;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class FishRodHook : DolocObject
{
	[SerializeField]
	private float biteDistance = 1.5f;

	[SerializeField]
	private Vector2 clipDistance = new Vector2(0.5f, 2f);

	[SerializeField]
	private float bounciness = 1f;

	[SerializeField]
	private float damping = 0.1f;

	[SerializeField]
	private FishShadow _fishShadow;

	private FloatingModel2D _floatingModel;

	private bool _isFloating;

	private SpriteRenderer _spriteRenderer;

	private Rigidbody2D _rigidbody;

	private bool _hasRaiseWaterSplash;

	public FishingPool CurrentFishPool { get; private set; }

	public Sprite HookSprite
	{
		get
		{
			return _spriteRenderer.sprite;
		}
		set
		{
			_spriteRenderer.sprite = value;
		}
	}

	public bool RBEnabled
	{
		get
		{
			return _rigidbody.simulated;
		}
		set
		{
			_rigidbody.simulated = value;
			_rigidbody.velocity = Vector2.zero;
		}
	}

	public Vector2 Velocity
	{
		get
		{
			return _rigidbody.velocity;
		}
		set
		{
			_rigidbody.velocity = value;
		}
	}

	public event Action<FishingPool> OnHooked;

	protected override void __Init()
	{
		base.__Init();
		_rigidbody = GetComponent<Rigidbody2D>();
		_spriteRenderer = GetComponent<SpriteRenderer>();
		SetVisible(value: false);
	}

	public void DisableBattle()
	{
		_fishShadow.SetVisible(value: false);
		_fishShadow.Stop();
	}

	public void DisableFloating()
	{
		_isFloating = false;
	}

	public void EnableFishShadow(Vector2 rodPosition)
	{
		RBEnabled = false;
		Vector2 vector = base.transform.position;
		_fishShadow.Hook = base.transform;
		_fishShadow.transform.position = vector + new Vector2(0f, -1f) * biteDistance;
		_fishShadow.SetVisible(value: true);
		_fishShadow.Battle(rodPosition);
		_isFloating = false;
	}

	public void HideFishShadow()
	{
		_fishShadow.SetVisible(value: false);
	}

	public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
	{
		RBEnabled = true;
		_rigidbody.AddForce(force, mode);
	}

	public void ResetHook()
	{
		CurrentFishPool = null;
		_hasRaiseWaterSplash = false;
		DisableBattle();
		RBEnabled = true;
	}

	private void StartFloatingPhysical()
	{
		if (_floatingModel == null)
		{
			_floatingModel = new FloatingModel2D(bounciness, damping);
		}
		_floatingModel.Reset();
		_isFloating = true;
	}

	private void TryRaiseWaterSplash(FishingPool fishingPool)
	{
		if (_hasRaiseWaterSplash || fishingPool.WaterEntity == null)
		{
			return;
		}
		fishingPool.WaterEntity.Splash(base.transform.position);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_FISHING_HOOK_SPLASH);
		Sequence s = DOTween.Sequence();
		for (int i = 0; i < UnityEngine.Random.Range(1, 4); i++)
		{
			s.AppendCallback(delegate
			{
				RaiseWaterBubbles(base.transform.position, 1f);
			});
			s.AppendInterval(0.12f);
		}
		_hasRaiseWaterSplash = true;
	}

	private void RaiseWaterBubbles(Vector2 position, float range)
	{
		Vector2 vector = new Vector2(UnityEngine.Random.Range(0f - range, range), 0f - range);
		DolocAPI.RaiseInstantPSEffects(position + vector, InstantParticleEffectsType.WATER_BUBBLES_SHALLOW);
	}

	private void FixedUpdate()
	{
		if (_isFloating)
		{
			float offset = Mathf.Clamp(CurrentFishPool.WaterEntity.waterController.CalcDistanceError(base.transform.position), 0f - clipDistance.x, clipDistance.y);
			Vector3 vector = base.transform.position;
			vector.y += _floatingModel.Update(offset, Time.fixedDeltaTime);
			base.transform.position = vector;
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (CurrentFishPool != null)
		{
			return;
		}
		CurrentFishPool = other.GetComponent<FishingPool>();
		if (CurrentFishPool != null)
		{
			TryRaiseWaterSplash(CurrentFishPool);
			if (CurrentFishPool.WaterEntity == null)
			{
				Debug.LogWarning("目标池塘\"" + other.gameObject.name + "\"未绑定水体");
				return;
			}
			_rigidbody.velocity = Vector2.zero;
			_rigidbody.simulated = false;
			StartFloatingPhysical();
			this.OnHooked?.Invoke(CurrentFishPool);
		}
	}
}
