using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[GameEntityManager("/dungeon/monster/nian_tail", DolocGameAssets.GAME_ENTITY_MONSTER_NIAN_TAIL_PART)]
public class NianTail : GameEntity, IAttackable, IFellable, IBombInteractive
{
	[SerializeField]
	private Light2D tailLight;

	[SerializeField]
	private GameObject outline;

	private SecondOrderSystem _followSystem;

	private Shiner _shiner;

	private Shaker _shaker;

	private MonsterController _monsterController;

	private MonsterDecoratorNian _monsterDecoratorNian;

	private Tween _flickerAnimation;

	private bool _interactWithGround;

	public bool ShouldFollow { get; set; }

	public bool ShowOutline
	{
		get
		{
			return outline.activeSelf;
		}
		set
		{
			outline.SetActive(value);
		}
	}

	public AttackableType attackableType => AttackableType.Enemy;

	public bool ShouldCostEnergy => false;

	public bool ShouldCostChopCounter => true;

	protected override void __Init()
	{
		base.__Init();
		_followSystem = GetComponent<SecondOrderSystem>();
		_shiner = new Shiner(GetComponent<SpriteRenderer>());
		_shaker = new Shaker(base.transform);
		ShowOutline = false;
	}

	public void ConfigureTail(MonsterController controller, Transform followTarget)
	{
		_monsterController = controller;
		_monsterDecoratorNian = controller.GetComponent<MonsterDecoratorNian>();
		_followSystem.SetTarget(followTarget, Vector2.zero);
	}

	private void FixedUpdate()
	{
		if (!(_monsterController == null) && ShouldFollow)
		{
			float num = _followSystem.OnFixedUpdate(Time.fixedDeltaTime);
			base.transform.localScale = new Vector3((!(num > 0f)) ? 1 : (-1), 1f, 1f);
		}
	}

	public void Drop(Vector2 force)
	{
		_interactWithGround = true;
		Rigidbody2D component = GetComponent<Rigidbody2D>();
		component.gravityScale = 6f;
		component.AddForce(force, ForceMode2D.Impulse);
		ShouldFollow = false;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (_interactWithGround && BattleUtils.IsRealWall(other))
		{
			_interactWithGround = false;
			Rigidbody2D component = GetComponent<Rigidbody2D>();
			component.velocity = Vector2.zero;
			component.gravityScale = 0f;
			base.gameObject.layer = LayerMask.NameToLayer("PlatformExclude");
			_followSystem.ResetPosition(base.transform.position);
			DolocAPI.cameraController.ShakeScreen();
			DolocAPI.RaiseInstantAnimEffects(other.ClosestPoint(base.transform.position), InstAnimEffectType.LARGE_SMOKE);
		}
	}

	private static int CalcTotalFlickerTimes(float duration, float stepInterval)
	{
		return Mathf.FloorToInt(Mathf.Sqrt(2f * duration / stepInterval + 0.25f) - 0.5f);
	}

	public void Flicker(float duration, float stepInterval = 0.1f, Action callback = null)
	{
		_flickerAnimation?.Kill();
		if (tailLight == null)
		{
			return;
		}
		float originalIntensity = tailLight.intensity;
		Sequence sequence = DOTween.Sequence();
		int num = CalcTotalFlickerTimes(duration, stepInterval);
		float num2 = (float)(num + 1) * stepInterval;
		for (int i = 0; i < num; i++)
		{
			float duration2 = num2 * 0.5f;
			sequence.Append(DOTween.To(() => tailLight.intensity, delegate(float x)
			{
				tailLight.intensity = x;
			}, 0f, duration2).SetEase(Ease.OutQuad));
			sequence.Append(DOTween.To(() => tailLight.intensity, delegate(float x)
			{
				tailLight.intensity = x;
			}, originalIntensity, duration2).SetEase(Ease.InQuad));
			num2 -= stepInterval;
		}
		sequence.OnComplete(delegate
		{
			tailLight.intensity = originalIntensity;
			_flickerAnimation = null;
			callback?.Invoke();
		});
		_flickerAnimation = sequence;
	}

	public void Hurt()
	{
		_shaker.Shake();
		_shiner.Raise(LocMaterials.GAME_MAT_HIT, 0.1f);
	}

	public bool OnAttacked(float attack, bool isCritical, Vector2 position, out bool isDead)
	{
		isDead = false;
		return _monsterDecoratorNian.OnTailAttacked(attack, isCritical, position);
	}

	public bool OnSwordAttack(float atk, bool isCritical, Vector2 position, out bool isDead)
	{
		return OnAttacked(atk, isCritical, position, out isDead);
	}

	public bool OnFell(ItemTool tool, Vector2 hitPosition)
	{
		_monsterDecoratorNian.OnTailFell(tool, hitPosition);
		return true;
	}

	public void OnBomb(float damage, bool criticalRate, Vector2 position)
	{
		_monsterDecoratorNian.OnTailBombed(damage, criticalRate, position);
	}
}
