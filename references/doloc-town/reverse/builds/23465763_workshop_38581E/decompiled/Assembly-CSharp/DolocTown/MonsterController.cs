using System;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(MonsterDecorator))]
[RequireComponent(typeof(MonsterRenderer))]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_BEE, Alias = "bee")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_DRONE, Alias = "drone")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_DRONE_EX, Alias = "drone_ex")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_BALL_DRONE, Alias = "ball_drone")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_CHOMPER, Alias = "chomper")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_SEED_CARRIER, Alias = "seed_carrier")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_TARDIGRADE, Alias = "tardigrade")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_FWB_DRONE, Alias = "fwb_drone")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_BOMBER, Alias = "bomber")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_AIRCRAFT, Alias = "aircraft")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_AMOEBA, Alias = "amoeba")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_AMOEBA_WET_LAND, Alias = "amoeba_wet_land")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_SCARECROW, Alias = "scarecrow")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_TARGET_01, Alias = "target_01")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_FUNGUS, Alias = "fungus")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_NIAN, Alias = "nian")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_NIAN_HEAD, Alias = "nian_head")]
[GameEntityManager("/dungeon/monster", DolocGameAssets.GAME_ENTITY_MONSTER_NIAN_TAIL, Alias = "nian_tail")]
public class MonsterController : GameEntity, IApc, IAttackable, IFellable, IBombInteractive, IInteractable
{
	private bool disposableAnimationEvent;

	private Action animationEvtCallback;

	[SerializeField]
	private Transform _firePosition;

	[SerializeField]
	private Transform _autoAimTargetPosition;

	public readonly MonsterAttackBehaviourManager<MonsterAttackId> attackBehaviourManager = new MonsterAttackBehaviourManager<MonsterAttackId>();

	private BattleSystem _battleSystem;

	private MonsterDecorator _decorator;

	private bool _shouldRunAI;

	private readonly RSTimerLock _hurtTimer = new RSTimerLock();

	public virtual Vector2 FirePosition => (_firePosition == null) ? base.transform.position : _firePosition.position;

	public Vector2 EmotionPos
	{
		get
		{
			EmotionPositionControl component = GetComponent<EmotionPositionControl>();
			if (component == null)
			{
				return base.transform.position;
			}
			return component.EmotionOffset + (Vector2)base.transform.position;
		}
	}

	public MonsterMover mover { get; private set; }

	public Collider2D Collider { get; private set; }

	public MonsterRenderer Renderer { get; private set; }

	public MonsterEnv Env { get; private set; }

	public float HealthPercent
	{
		get
		{
			if (Monster == null)
			{
				return 0f;
			}
			return Monster.HealthPercent;
		}
	}

	public MonsterAI MonsterAI { get; private set; }

	public Monster Monster { get; private set; }

	public MonsterProto MonsterProto => Monster.proto;

	public MonsterDecorator Decorator => _decorator;

	public bool IsDirectional => Monster.proto.MoverProto.isDirectional;

	public string CurrentStateName => MonsterAI?.CurrentState.GetType().Name;

	public Vector2 PositionCenter => Collider.bounds.center;

	public Vector2 PositionAttack
	{
		get
		{
			if (_autoAimTargetPosition == null)
			{
				return PositionCenter;
			}
			return _autoAimTargetPosition.position;
		}
	}

	public bool IsJumping
	{
		get
		{
			if (mover is MonsterMoverGround monsterMoverGround)
			{
				return monsterMoverGround.IsJumping;
			}
			return false;
		}
	}

	public bool FaceRight
	{
		get
		{
			return base.transform.localScale.x > 0f;
		}
		set
		{
			Vector3 localScale = base.transform.localScale;
			localScale.x = MathF.Abs(localScale.x) * (float)(value ? 1 : (-1));
			base.transform.localScale = localScale;
		}
	}

	public bool ShieldBullet => _decorator.ShieldBullet;

	public AttackableType attackableType => _decorator.attackableType;

	public bool ShouldCostEnergy => false;

	public bool ShouldCostChopCounter => false;

	public bool OnlyTouch => true;

	public bool CanInteractContinues => false;

	public event Action OnHurtResumed;

	public void BindAnimationEvent(Action callback, bool disposable = true)
	{
		animationEvtCallback = callback;
		disposableAnimationEvent = disposable;
	}

	private void AnimationEvent()
	{
		animationEvtCallback?.Invoke();
		if (disposableAnimationEvent)
		{
			animationEvtCallback = null;
		}
	}

	private void InitAttackBehaviours(BattleSystem battleSystem)
	{
		IMonsterAttackBehaviour[] attackBehaviourProtos = Monster.proto.AttackBehaviourProtos;
		foreach (IMonsterAttackBehaviour monsterAttackBehaviour in attackBehaviourProtos)
		{
			if (monsterAttackBehaviour.AttackId != 0)
			{
				MonsterAttackBehaviour monsterAttackBehaviour2 = MonsterAttackBehaviourHelper.CreateMonsterAttackBehaviour(battleSystem, base.transform, monsterAttackBehaviour);
				if (monsterAttackBehaviour2 == null || !monsterAttackBehaviour2.Validate())
				{
					Debug.LogError($"怪物\"{Monster.proto.Name}\"的技能\"{monsterAttackBehaviour.GetType()}\"初始化失败");
				}
				else if (!attackBehaviourManager.RegisterAttackBehaviour(monsterAttackBehaviour.AttackId, monsterAttackBehaviour2))
				{
					Debug.LogError($"怪物\"{Monster.proto.Name}\"的技能\"{monsterAttackBehaviour.GetType()}\"重复注册");
				}
			}
		}
	}

	public void ReduceCD(MonsterAttackId attackId, float t)
	{
		attackBehaviourManager.ReduceCD(attackId, t);
	}

	public void IsAttacking<T>() where T : IMonsterAttackBehaviour
	{
	}

	public void Attack(MonsterAttackId id, Transform target, Action callback = null)
	{
		attackBehaviourManager.Attack(id, target, callback);
	}

	public bool IsAttackBehaviourAvailable(MonsterAttackId id)
	{
		return attackBehaviourManager.IsAvailable(id);
	}

	public bool IsAnyAttackBehaviourAvailable(out MonsterAttackId attackId)
	{
		return attackBehaviourManager.TryGetFirstAvailableAttackBehaviour(out attackId);
	}

	public bool TryGetFirstAvailableWorkableAttackBehaviour(Transform target, out MonsterAttackId id)
	{
		if (attackBehaviourManager.TryGetFirstAvailableAttackBehaviour(out id))
		{
			return IsAttackBehaviourWork(id, target);
		}
		return false;
	}

	public bool IsAttackBehaviourWorkAndAvailable(MonsterAttackId id, Transform target)
	{
		if (IsAttackBehaviourAvailable(id))
		{
			return IsAttackBehaviourWork(id, target);
		}
		return false;
	}

	public bool IsAttackBehaviourWork(MonsterAttackId id, Transform target)
	{
		if (target != null && attackBehaviourManager.TryGetAttackBehaviour(id, out var behaviour))
		{
			return behaviour.IsTargetInAttackRange(target);
		}
		return false;
	}

	protected override void __Init()
	{
		base.__Init();
		Renderer = GetComponent<MonsterRenderer>();
		Renderer.Init();
		Collider = GetComponent<Collider2D>();
		_decorator = GetComponent<MonsterDecorator>();
		_decorator.Init(this);
	}

	public void Run(MonsterEnv env, Monster monster, BattleSystem battleSystem)
	{
		Monster = monster;
		Monster.Controller = this;
		MonsterAI = MonsterAI.Create(MonsterProto.Name, this);
		position2d = monster.position;
		if (!monster.proto.IsAir && env.GroundMap.RaycastToGround(monster.position, out var result))
		{
			position2d = result;
		}
		_shouldRunAI = true;
		InitApc(battleSystem, env);
		_decorator.OnMonsterLoaded(monster);
		PostSoundEvent(MonsterProto.HummingSound);
		Env.TryAddMonster(this);
	}

	public void InitApc(BattleSystem battleSystem, MonsterEnv env)
	{
		MonsterProto proto = Monster.proto;
		_hurtTimer.SetInterval(proto.HurtDuration);
		_battleSystem = battleSystem;
		Env = env;
		_decorator.OnEnvironmentChanged(env);
		mover = MonsterMoverHelper.CreateMover(env, base.transform, Monster.proto.MoverProto, Monster.proto.IsAir);
		InitAttackBehaviours(battleSystem);
		MonsterAI.OnStart();
	}

	public void OnUpdate(float dt)
	{
		if (!_hurtTimer.IsLocked && _shouldRunAI)
		{
			MonsterAI.OnUpdate(dt);
			Renderer.OnUpdate(dt);
			_decorator.OnUpdate(dt);
		}
	}

	public void OnFixedUpdate(float dt)
	{
		attackBehaviourManager.UpdateCDCounter(dt);
		if (_hurtTimer.IsLocked)
		{
			if (_hurtTimer.Tick(dt))
			{
				this.OnHurtResumed?.Invoke();
			}
		}
		else
		{
			_decorator.OnFixedUpdate(dt);
			MonsterAI?.OnFixedUpdate(dt);
		}
	}

	public void OnPause()
	{
	}

	public void OnResume()
	{
	}

	public new void PostSoundEvent(string soundEvent)
	{
		DolocAPI.Sound.PostSoundEvent(soundEvent, base.gameObject);
	}

	public void PostSoundEvent(SoundEvents soundEvent)
	{
		DolocAPI.Sound.PostSoundEvent(soundEvent, base.gameObject);
	}

	public override void OnReuse()
	{
		base.OnReuse();
		_decorator.OnReuse();
		DolocAPI.Sound.RegisterGameObject(base.gameObject);
	}

	public override void OnRecycle()
	{
		MonsterAI.OnStop();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_MONSTER_BUS, base.gameObject);
		DolocAPI.Sound.UnregisterGameObject(base.gameObject);
		DolocAPI.ClearEmotions(base.transform);
		_decorator.OnRecycle();
		SetVisible(value: false);
		if (Monster != null)
		{
			Monster.position = position2d;
			Monster.Controller = null;
			Monster = null;
		}
		attackBehaviourManager.Dispose(_battleSystem);
		_battleSystem.RemoveBattleUnit(this);
	}

	public void Hurt(Transform attacker, float duration, bool clearStatus = false, bool stopController = true)
	{
		if (stopController && duration > 0f)
		{
			_hurtTimer.LockWithNewInterval(duration);
		}
		if (clearStatus)
		{
			attackBehaviourManager.StopAttackBehaviour();
			mover.StopMove();
		}
		Renderer.OnHurt();
		MonsterAI.OnHurt(attacker);
		PostSoundEvent(MonsterProto.HurtSound);
	}

	public void StopAttack()
	{
		attackBehaviourManager.StopAttackBehaviour();
		mover.StopMove();
	}

	public void Hurt(Transform attacker, bool clearStatus = false, bool stopController = true)
	{
		Hurt(attacker, MonsterProto.HurtDuration, clearStatus, stopController);
	}

	public virtual bool OnAttacked(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		return _decorator.OnAttacked(attack, criticalRate, pos, out isDead);
	}

	public virtual bool OnSwordAttack(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		return _decorator.OnSwordAttack(attack, criticalRate, pos, out isDead);
	}

	public virtual bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		return _decorator.OnFell(tool, hitPoint);
	}

	public void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
		_decorator.OnBomb(damage, criticalRate, pos);
	}

	public void OnDead()
	{
		_decorator.OnDead();
	}

	public void OnTouch()
	{
		MonsterAI.OnTouchByAgent();
	}

	public void OnDisTouch()
	{
	}

	public void OnInteract()
	{
	}

	public bool MoveTo(MoveTargetType target, Action endAction = null)
	{
		if (Monster == null || target == MoveTargetType.Custom)
		{
			return false;
		}
		GetComponent<Rigidbody2D>().velocity = Vector2.zero;
		if (mover.GetMoveTarget(target, Monster.proto.IsAir, out var target2))
		{
			return mover.TryMoveTo(target2, endAction);
		}
		return false;
	}

	public bool MoveTo(Transform transform, Action endAction = null)
	{
		if ((object)transform == null)
		{
			return false;
		}
		return MoveTo(transform.position);
	}

	public bool MoveTo(Vector2 position, Action endAction = null)
	{
		if (Monster == null)
		{
			return false;
		}
		GetComponent<Rigidbody2D>().velocity = Vector2.zero;
		return mover.TryMoveTo(position, endAction);
	}

	public bool MoveAround(Vector2 position, Action endAction = null)
	{
		if (mover.GetMoveTargetAround(position, out var target))
		{
			return MoveTo(target, endAction);
		}
		return false;
	}

	public bool Patrol(Vector2 patrolCenter, float patrolRadius, float aroundRange = 3f)
	{
		return MoveTo(GetPatrolPosition(patrolCenter, patrolRadius, aroundRange));
	}

	public Vector2 GetPatrolPosition(Vector2 center, float radius, float aroundRange = 3f)
	{
		Vector2 vector = base.transform.position;
		float num = Vector2.Distance(center, vector);
		if (num > radius)
		{
			return RandomPositionInCircle(center, radius);
		}
		Vector2 normalized = (vector - center).normalized;
		Vector2 vector2 = (radius - num) * 2f * normalized + vector;
		return RandomPositionInCircle(vector2, aroundRange);
	}

	private Vector2 RandomPositionInCircle(Vector2 position, float radius)
	{
		return (Vector2)UnityEngine.Random.insideUnitSphere * radius + position;
	}

	public void StopMove()
	{
		mover.StopMove();
	}
}
