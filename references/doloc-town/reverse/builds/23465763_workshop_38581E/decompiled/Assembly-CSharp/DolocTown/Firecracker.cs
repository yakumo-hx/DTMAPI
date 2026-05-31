using System.Linq;
using Cysharp.Threading.Tasks;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Firecracker : Skill
{
	[SerializeField]
	[Min(0f)]
	[Tooltip("超出时间如果炸弹仍未碰到地面则会直接回收")]
	private float lifeTime = 10f;

	[SerializeField]
	private float firecrackerInitialInterval = 1f;

	[SerializeField]
	[Min(0.05f)]
	private float firecrackerInterval = 0.2f;

	[SerializeField]
	[Min(1f)]
	private int firecrackerMaxBombCount = 10;

	[SerializeField]
	private Sprite[] firecrackerSprites;

	private float _lifeTimeCounter;

	private bool _shouldQuit;

	private float _criticalRate;

	private bool _startedFirecracker;

	private RSTimer _firecrackerTimer;

	private Counter _firecrackerBombCounter;

	private bool _stuckIntoGround;

	private Sprite _originSprite;

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
		_originSprite = GetComponent<SpriteRenderer>().sprite;
	}

	private Sprite GetSprite(int index)
	{
		if (firecrackerSprites == null || firecrackerSprites.Length == 0)
		{
			return _originSprite;
		}
		if (index < 0 || index >= firecrackerSprites.Length)
		{
			return firecrackerSprites.Last();
		}
		return firecrackerSprites[index];
	}

	public void AddForce(Vector2 force)
	{
		_rigidbody2D.AddForce(force, ForceMode2D.Impulse);
	}

	public void StartFireCracker(float interval, int maxBombCount = 10)
	{
		_startedFirecracker = true;
		_firecrackerTimer = new RSTimer(interval);
		_firecrackerBombCounter = new Counter(maxBombCount);
	}

	public override bool OnStart()
	{
		_lifeTimeCounter = 0f;
		_shouldQuit = false;
		_startedFirecracker = false;
		GetComponent<SpriteRenderer>().sprite = _originSprite;
		UniTask.Delay(Mathf.RoundToInt(firecrackerInitialInterval * 650f)).ContinueWith(delegate
		{
			StartFireCracker(firecrackerInterval, firecrackerMaxBombCount);
		}).Forget();
		return true;
	}

	public override bool OnFixedUpdate(float dt)
	{
		if (IsStuckInGround())
		{
			Vector2Int pos = DolocAPI.CurrentRoom.Geometry.CalcCellPosition(base.transform.position);
			Vector2Int nearestEmptyPosition = DolocAPI.CurrentRoom.Geometry.GetNearestEmptyPosition(pos);
			base.transform.position = DolocAPI.CurrentRoom.Geometry.CalcWorldPositionCenter(nearestEmptyPosition);
		}
		if (_startedFirecracker && _firecrackerTimer.Tick(dt))
		{
			DolocAPI.EntitySystem.Next<Boom>().Invoke(base.transform.position, Damage, CriticalRate, shouldRaiseScreenTwist: false, shouldWorkOnMonster: true, shouldWorkOnPlayer: false, shouldWorkOnEnvironment: false, OnHitSomething, CustomEffects);
			GetComponent<SpriteRenderer>().sprite = GetSprite(_firecrackerBombCounter.Value);
			_shouldQuit = _firecrackerBombCounter.Tick();
		}
		if (_lifeTimeCounter >= lifeTime)
		{
			return true;
		}
		_lifeTimeCounter += dt;
		return _shouldQuit;
	}

	private bool IsStuckInGround()
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom == null || currentRoom.IsInHouse)
		{
			return false;
		}
		Vector2Int cellPos = currentRoom.Geometry.CalcCellPosition(base.transform.position);
		return currentRoom.Geometry.IsObstacle(cellPos);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		base.RB.isKinematic = false;
		_stuckIntoGround = false;
	}

	public void OnHitSomething(Collider2D something)
	{
		MonsterDecoratorNian component = something.GetComponent<MonsterDecoratorNian>();
		if (!(component == null))
		{
			component.Drop(base.transform.position);
		}
	}

	public void CustomEffects(Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.SPARKS);
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.LANTERN_DEBRIS);
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.SMOKE_BRUST_01);
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.ELECTRIC_SPARKS);
	}
}
