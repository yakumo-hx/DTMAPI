using Cysharp.Threading.Tasks;
using DG.Tweening;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterDecoratorNian : MonsterDecorator
{
	[SerializeField]
	private NianConfig nianConfig;

	[SerializeField]
	private Transform connectionPosition;

	[SerializeField]
	private bool firstStage;

	[SerializeField]
	private GameObject borderRenderer;

	[SerializeField]
	[Tooltip("至多等待坠落的时间")]
	private float maxDropDuration = 2f;

	private const string MONSTER_NAME = "nian";

	private readonly RSTimerLock _immunityTimer = new RSTimerLock();

	private int _fellCount;

	private bool _interactWithGround;

	private bool _isGroundCheckAvailable;

	private bool _isWaitingForDrop;

	private float _dropTimer;

	private MonsterSight _sight;

	private readonly GameEntitySlot<NianTail> _tailSlot = new GameEntitySlot<NianTail>();

	private bool IsLastNianInRoom
	{
		get
		{
			foreach (Monster allMonster in base.Controller.Env.AllMonsters)
			{
				if (allMonster != base.Controller.Monster && allMonster.Name.Contains("nian"))
				{
					return false;
				}
			}
			return true;
		}
	}

	public override bool ShouldGenDropItems => IsLastNianInRoom;

	public override bool SendDeadEvent => false;

	private float yForce
	{
		get
		{
			if (!(nianConfig != null))
			{
				return 30f;
			}
			return nianConfig.boomForce.y;
		}
	}

	private float xForce
	{
		get
		{
			if (!(nianConfig != null))
			{
				return 9f;
			}
			return nianConfig.boomForce.x;
		}
	}

	private float toolAttack
	{
		get
		{
			if (!(nianConfig != null))
			{
				return 25f;
			}
			return nianConfig.toolAttack;
		}
	}

	private int maxFellCount
	{
		get
		{
			if (!(nianConfig != null))
			{
				return 3;
			}
			return nianConfig.maxChopCount;
		}
	}

	public bool ShowOutline
	{
		set
		{
			borderRenderer.SetActive(value);
			_tailSlot.DoIfExist(delegate(NianTail x)
			{
				x.ShowOutline = value;
			});
		}
	}

	public bool InteractWithGround
	{
		get
		{
			return _interactWithGround;
		}
		set
		{
			_interactWithGround = value;
			Rigidbody2D component = GetComponent<Rigidbody2D>();
			if (value)
			{
				component.gravityScale = 6f;
				component.sharedMaterial = DolocAPI.gameConfig.MonsterPhysicsMaterial2D;
				base.gameObject.layer = LayerMask.NameToLayer("PlatformExclude");
			}
			else
			{
				component.sharedMaterial = null;
				component.gravityScale = 0f;
				component.velocity = Vector2.zero;
				base.gameObject.layer = LayerMask.NameToLayer("Enemy");
			}
		}
	}

	private void InitFirstStage()
	{
		if (firstStage)
		{
			_tailSlot.Do(delegate(NianTail tail)
			{
				tail.ConfigureTail(base.Controller, connectionPosition);
				tail.ShouldFollow = true;
			});
		}
	}

	protected override void OnInitDecorator()
	{
		base.OnInitDecorator();
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		SpriteRenderer component2 = borderRenderer.GetComponent<SpriteRenderer>();
		component2.sortingLayerName = component.sortingLayerName;
		component2.sortingOrder = component.sortingOrder;
		_sight = base.Controller.GetComponentInChildren<MonsterSight>(includeInactive: true);
		ShowOutline = false;
		InitFirstStage();
		NianTailLight componentInChildren = GetComponentInChildren<NianTailLight>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.Init();
		}
	}

	public override void OnFixedUpdate(float dt)
	{
		LongDropTesting(dt);
		RoomRangeCheck();
		if (_immunityTimer.IsLocked && _immunityTimer.Tick(dt))
		{
			ShowOutline = false;
		}
	}

	public override void OnMonsterLoaded(Monster monster)
	{
		if (firstStage)
		{
			_tailSlot.Do(delegate(NianTail tail)
			{
				tail.ConfigureTail(base.Controller, connectionPosition);
				tail.ShouldFollow = true;
			});
		}
	}

	public override void OnReuse()
	{
		base.OnReuse();
		InitFirstStage();
		NianTailLight componentInChildren = GetComponentInChildren<NianTailLight>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.OnReuse();
		}
	}

	private bool TryEnterDropState()
	{
		MonsterAI monsterAI = base.Controller.MonsterAI;
		if (!(monsterAI is MonsterAI_Nian monsterAI_Nian))
		{
			if (!(monsterAI is MonsterAI_NianHead monsterAI_NianHead))
			{
				if (monsterAI is MonsterAI_NianTail monsterAI_NianTail)
				{
					if (monsterAI_NianTail.CheckState<MonsterAI_NianTail.DropState>(out var _))
					{
						return false;
					}
					monsterAI_NianTail.EnterState<MonsterAI_NianTail.DropState>();
					return true;
				}
				return false;
			}
			if (monsterAI_NianHead.CheckState<MonsterAI_NianHead.DropState>(out var _))
			{
				return false;
			}
			monsterAI_NianHead.EnterState<MonsterAI_NianHead.DropState>();
			return true;
		}
		if (monsterAI_Nian.CheckState<MonsterAI_Nian.DropState>(out var _))
		{
			return false;
		}
		monsterAI_Nian.EnterState<MonsterAI_Nian.DropState>();
		return true;
	}

	private bool TryCountDownDropState(float duration)
	{
		MonsterAI monsterAI = base.Controller.MonsterAI;
		if (!(monsterAI is MonsterAI_Nian monsterAI_Nian))
		{
			if (!(monsterAI is MonsterAI_NianHead monsterAI_NianHead))
			{
				if (monsterAI is MonsterAI_NianTail monsterAI_NianTail)
				{
					if (monsterAI_NianTail.CheckState<MonsterAI_NianTail.DropState>(out var value))
					{
						value.StartCountdown(duration);
						return true;
					}
					return false;
				}
				return false;
			}
			if (monsterAI_NianHead.CheckState<MonsterAI_NianHead.DropState>(out var value2))
			{
				value2.StartCountdown(duration);
				return true;
			}
			return false;
		}
		if (monsterAI_Nian.CheckState<MonsterAI_Nian.DropState>(out var value3))
		{
			value3.StartCountdown(duration);
			return true;
		}
		return false;
	}

	private void TryQuitDropState()
	{
		MonsterAI monsterAI = base.Controller.MonsterAI;
		MonsterAI_Nian.DropState value3;
		if (!(monsterAI is MonsterAI_Nian monsterAI_Nian))
		{
			MonsterAI_NianHead.DropState value2;
			if (!(monsterAI is MonsterAI_NianHead monsterAI_NianHead))
			{
				if (monsterAI is MonsterAI_NianTail monsterAI_NianTail && monsterAI_NianTail.CheckState<MonsterAI_NianTail.DropState>(out var _))
				{
					monsterAI_NianTail.EnterState<MonsterAI_NianTail.WanderState>();
				}
			}
			else if (monsterAI_NianHead.CheckState<MonsterAI_NianHead.DropState>(out value2))
			{
				monsterAI_NianHead.EnterState<MonsterAI_NianHead.WanderState>();
			}
		}
		else if (monsterAI_Nian.CheckState<MonsterAI_Nian.DropState>(out value3))
		{
			monsterAI_Nian.EnterState<MonsterAI_Nian.WanderState>();
		}
	}

	private bool IsDropAvailable()
	{
		if (!_immunityTimer.IsLocked)
		{
			return !base.Controller.attackBehaviourManager.IsAttackingOfType<IBulletTrain>();
		}
		return false;
	}

	public void Drop(Vector2 position)
	{
		if (!IsDropAvailable() || !TryEnterDropState())
		{
			return;
		}
		_sight.Collider.enabled = false;
		_isWaitingForDrop = true;
		_dropTimer = 0f;
		InteractWithGround = true;
		_isGroundCheckAvailable = false;
		UniTask.Delay(600).ContinueWith(() => _isGroundCheckAvailable = true).Forget();
		Rigidbody2D component = GetComponent<Rigidbody2D>();
		float sign = Mathf.Sign(base.transform.position.x - position.x);
		component.AddForce(new Vector2(xForce * sign, yForce), ForceMode2D.Impulse);
		GetComponentInChildren<PhysicalDamageBox>().Enabled = false;
		GetComponentInChildren<MonsterSight>().Collider.enabled = false;
		if (firstStage)
		{
			DolocAPI.cameraController.ShakeScreen(0.3f);
			DolocAPI.RaiseScreenTwist(connectionPosition.position, 0.3f);
			DolocAPI.RaiseInstantPSEffects(connectionPosition.position, InstantParticleEffectsType.SMOKE_BRUST_01);
			DolocAPI.RaiseInstantPSEffects(connectionPosition.position, InstantParticleEffectsType.ELECTRIC_SPARKS);
			DolocAPI.RaiseInstantPSEffects(connectionPosition.position, InstantParticleEffectsType.DRONE_PARTS_SM_01);
			DolocAPI.RaiseInstantAnimEffects(connectionPosition.position, InstAnimEffectType.ELECTRIC_CURRENT);
			_tailSlot.DoIfExist(delegate(NianTail tail)
			{
				tail.Drop(new Vector2((0f - xForce) * sign, yForce));
			});
		}
	}

	private void RoomRangeCheck()
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		if (!currentRoom.Geometry.ExtendContains(base.Controller.position2d, 5f))
		{
			Vector2 vector = currentRoom.Geometry.Constarint(base.Controller.position2d, 5f);
			Vector2Int pos = currentRoom.Geometry.CalcCellPosition(vector);
			if (currentRoom.Geometry.TryGetNearestGroundPosition(pos, out var targetPosition))
			{
				targetPosition.y += 2;
				base.Controller.position2d = currentRoom.Geometry.CalcWorldPosition(targetPosition);
			}
			else
			{
				base.Controller.position2d = vector;
			}
		}
	}

	private void LongDropTesting(float dt)
	{
		if (_isWaitingForDrop)
		{
			_dropTimer += dt;
			if (_dropTimer >= maxDropDuration)
			{
				_isWaitingForDrop = false;
				InteractWithGround = false;
				TryQuitDropState();
			}
		}
	}

	public void OnExitDropState()
	{
		if (firstStage)
		{
			_tailSlot.DoIfExist(delegate(NianTail tail)
			{
				tail.ShouldFollow = true;
			});
		}
		_immunityTimer.LockWithNewInterval(nianConfig.firecrackersImmunityTime);
		ShowOutline = true;
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		InteractWithGround = false;
		_tailSlot.Release();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!InteractWithGround || !_isGroundCheckAvailable || !BattleUtils.IsRealWall(other))
		{
			return;
		}
		_sight.Collider.enabled = false;
		_isWaitingForDrop = false;
		InteractWithGround = false;
		_fellCount = maxFellCount;
		DolocAPI.RaiseInstantAnimEffects(other.ClosestPoint(base.transform.position), InstAnimEffectType.LARGE_SMOKE);
		DolocAPI.cameraController.ShakeScreen();
		if (!TryCountDownDropState(nianConfig.downTime) || !firstStage)
		{
			return;
		}
		_tailSlot.DoIfExist(delegate(NianTail x)
		{
			x.Flicker(nianConfig.downTime - 1f, 0.1f, delegate
			{
				DolocAPI.RaiseInstantAnimEffects(GetComponent<EmotionPositionControl>().EmotionPosition, InstAnimEffectType.DANGER_WARNING_02);
			});
		});
	}

	public bool OnTailAttacked(float attack, bool isCritical, Vector2 position)
	{
		int dmg = BattleUtils.CalcDamage(attack, base.Controller.MonsterProto.Defense, isCritical);
		if (base.Controller.Monster.ManualDamage(dmg))
		{
			return true;
		}
		_tailSlot.DoIfExist(delegate(NianTail tail)
		{
			tail.Hurt();
			DolocAPI.RaiseDamageTip(dmg, tail.position, isCritical);
		});
		return true;
	}

	public void OnTailFell(ItemTool tool, Vector2 pos)
	{
		int num = BattleUtils.CalcDamage((_fellCount-- > 0) ? toolAttack : ((float)tool.Attack), nianConfig.downDefense, isCritical: true);
		if (!base.Controller.Monster.ManualDamage(num))
		{
			_tailSlot.DoIfExist(delegate(NianTail tail)
			{
				tail.Hurt();
			});
			DolocAPI.RaiseDamageTip(num, pos, isHeavy: true);
			if (_fellCount <= 0)
			{
				TryQuitDropState();
			}
		}
	}

	public void OnTailBombed(float damage, bool criticalRate, Vector2 pos)
	{
		int dmg = BattleUtils.CalcDamage(damage, base.Controller.MonsterProto.Defense, criticalRate);
		if (!base.Controller.Monster.ManualDamage(dmg))
		{
			_tailSlot.DoIfExist(delegate(NianTail tail)
			{
				tail.Hurt();
				DolocAPI.RaiseDamageTip(dmg, tail.position, criticalRate);
			});
		}
	}

	public override bool OnFell(ItemTool tool, Vector2 pos)
	{
		float attack = ((_fellCount-- > 0) ? toolAttack : ((float)tool.Attack));
		if (!base.Controller.Monster.Damage(attack, critical: true, nianConfig.downDefense, out var value))
		{
			base.Controller.Hurt(DolocAPI.AgentTransform, clearStatus: false, stopController: false);
		}
		DolocAPI.RaiseDamageTip(value, pos, isHeavy: true);
		if (_fellCount <= 0)
		{
			TryQuitDropState();
		}
		return true;
	}

	public override void OnDead()
	{
		InvokeDeadEffects();
		if (!firstStage)
		{
			if (IsLastNianInRoom)
			{
				DolocAPI.BroadcastString(GameEventType.CUSTOM, nianConfig.deathEventName);
				DolocAPI.BroadcastString(GameEventType.SLAIN_MONSTER, "nian");
				DolocAPI.archiveHandle.RecordCollection(CollectionType.Monster, "nian");
				DolocAPI.AddBattleExp(10);
			}
			return;
		}
		Vector3 position = _tailSlot.Entity.position;
		Vector3 localScale = _tailSlot.Entity.transform.localScale;
		if (base.Controller.Env.CallMonster("nian_tail", out var monster))
		{
			monster.Controller.position2d = position;
			monster.Controller.transform.localScale = localScale;
		}
		if (base.Controller.Env.CallMonster("nian_head", out var monster2))
		{
			monster2.Controller.position2d = base.Controller.position2d;
			monster2.Controller.transform.localScale = base.Controller.transform.localScale;
		}
	}

	private void InvokeDeadEffects()
	{
		_InvokeDeadEffects(nianConfig.deathEffects);
	}

	private void _InvokeDeadEffects(IEffects effects, float interval = 0.13f)
	{
		if (effects == null)
		{
			return;
		}
		Vector3 position = base.Controller.transform.position;
		Sequence s = DOTween.Sequence();
		for (int i = 0; i < 5; i++)
		{
			Vector3 randomAroundPosition = Random.insideUnitSphere.normalized * Random.Range(1, 4) + position;
			s.AppendCallback(delegate
			{
				effects.Raise(randomAroundPosition);
			});
			s.AppendInterval(interval);
		}
	}
}
