using DolocTown.Config;
using DolocTown.Config.Animal;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.UI;
using RedSaw;
using RedSaw.AI.StateMachine;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class DecorativeAnimal : InteractableObject, IRecyclable
{
	private abstract class DecorativeAnimalState : State
	{
		protected readonly DecorativeAnimal animal;

		protected readonly RedSaw.AI.StateMachine.StateMachine stateMachine;

		protected readonly RSTimer quitCheckerTimer = new RSTimer();

		protected bool ShouldQuit { get; private set; }

		protected DecorativeAnimalState(DecorativeAnimal animal, RedSaw.AI.StateMachine.StateMachine stateMachine)
			: base(stateMachine)
		{
			this.animal = animal;
		}

		protected void UpdateShouldQuit(float dt)
		{
			if (quitCheckerTimer.Tick(dt))
			{
				ShouldQuit = GetShouldQuit();
			}
		}

		protected void PlayAnimation(string anim, bool force)
		{
			animal.PlayAnimation(anim, force);
		}
	}

	[State("decorative_animal", true)]
	private class HideState : DecorativeAnimalState
	{
		private bool shouldLeaveVoid;

		public HideState(DecorativeAnimal animal, RedSaw.AI.StateMachine.StateMachine sm)
			: base(animal, sm)
		{
		}

		public override State GetNextState(float dt)
		{
			if (!shouldLeaveVoid)
			{
				return null;
			}
			return GetState<AppearState>();
		}

		public override void OnEnter()
		{
			Debug.Log("Enter HideState");
			shouldLeaveVoid = false;
			animal.HideAnimal();
		}

		protected override void OnFixedUpdate(float dt)
		{
			if (quitCheckerTimer.Tick(dt))
			{
				shouldLeaveVoid = !GetShouldQuit();
			}
		}
	}

	[State("decorative_animal", false)]
	private class AppearState : DecorativeAnimalState
	{
		private float waitDuration;

		public AppearState(DecorativeAnimal animal, RedSaw.AI.StateMachine.StateMachine stateMachine)
			: base(animal, stateMachine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (waitDuration > 0f)
			{
				return null;
			}
			return GetState<IdleState>();
		}

		protected override void OnFixedUpdate(float dt)
		{
			if (waitDuration >= 0f)
			{
				waitDuration -= dt;
			}
		}

		public override void OnEnter()
		{
			Debug.Log("Enter AppearState");
			waitDuration = Random.Range(0, 4);
		}

		public override void OnExit()
		{
			animal.ShowAnimal();
		}
	}

	[State("decorative_animal", false)]
	private class IdleState : DecorativeAnimalState
	{
		private readonly RSTimer timer;

		private bool shouldEndState;

		public IdleState(DecorativeAnimal animal, RedSaw.AI.StateMachine.StateMachine stateMachine)
			: base(animal, stateMachine)
		{
			timer = new RSTimer();
		}

		public void SetWaitDuration(float duration)
		{
			timer.SetInterval(duration);
		}

		public override void OnEnter()
		{
			animal.PlayAnimation("idle");
			timer.SetInterval(Random.Range(2, 4));
			shouldEndState = false;
		}

		public override State GetNextState(float dt)
		{
			if (shouldEndState)
			{
				if (GetShouldQuit())
				{
					return GetState<LeaveState>();
				}
				if (RandomUtils.Dice(0.1f) && animal.TrySetFeederPoint())
				{
					return GetState<EatState>();
				}
				return GetState<WanderState>();
			}
			return null;
		}

		protected override void OnFixedUpdate(float dt)
		{
			if (!shouldEndState && timer.Tick(dt))
			{
				shouldEndState = true;
			}
		}
	}

	[State("decorative_animal", false)]
	private class WanderState : DecorativeAnimalState
	{
		private float currentDestination;

		private Vector2 moveDelta;

		public WanderState(DecorativeAnimal animal, RedSaw.AI.StateMachine.StateMachine stateMachine)
			: base(animal, stateMachine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (base.ShouldQuit)
			{
				return GetState<LeaveState>();
			}
			if (!(Mathf.Abs(animal.transform.position.x - currentDestination) < 0.5f))
			{
				return null;
			}
			return GetState<IdleState>();
		}

		public override void OnEnter()
		{
			animal.PlayAnimation("move");
			currentDestination = animal.RandomPoint.x;
			float f = currentDestination - animal.transform.position.x;
			float dst = Mathf.Abs(f);
			float moveSpeed = animal.GetMoveSpeed(dst);
			moveDelta = new Vector2(Mathf.Sign(f) * moveSpeed, 0f);
			animal.transform.localScale = new Vector3(0f - Mathf.Sign(f), 1f, 1f);
		}

		protected override void OnFixedUpdate(float dt)
		{
			UpdateShouldQuit(dt);
			animal.transform.Translate(moveDelta * dt);
		}
	}

	[State("decorative_animal", false)]
	private class LeaveState : DecorativeAnimalState
	{
		private float dest;

		private Vector2 moveDelta;

		private bool hasReach;

		private float waitDuration;

		public LeaveState(DecorativeAnimal animal, RedSaw.AI.StateMachine.StateMachine stateMachine)
			: base(animal, stateMachine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (hasReach)
			{
				if (waitDuration > 0f)
				{
					return null;
				}
				return GetState<HideState>();
			}
			if (Mathf.Abs(dest - animal.position2d.x) < 0.5f)
			{
				hasReach = true;
				animal.PlayAnimation("idle");
			}
			return null;
		}

		protected override void OnFixedUpdate(float dt)
		{
			if (hasReach)
			{
				waitDuration -= dt;
			}
			else
			{
				animal.transform.Translate(moveDelta * dt);
			}
		}

		public override void OnEnter()
		{
			hasReach = false;
			waitDuration = Random.Range(0, 2);
			animal.PlayAnimation("move");
			dest = animal.transitionPoint.x;
			float f = dest - animal.transform.position.x;
			float dst = Mathf.Abs(f);
			float moveSpeed = animal.GetMoveSpeed(dst);
			moveDelta = new Vector2(Mathf.Sign(f) * moveSpeed, 0f);
			animal.transform.localScale = new Vector3(0f - Mathf.Sign(f), 1f, 1f);
		}
	}

	[State("decorative_animal", false)]
	private class EatState : DecorativeAnimalState
	{
		private bool hasReach;

		private float eatDuration;

		private float currentDestination;

		private Vector2 moveDelta;

		public EatState(DecorativeAnimal animal, RedSaw.AI.StateMachine.StateMachine stateMachine)
			: base(animal, stateMachine)
		{
		}

		public override State GetNextState(float dt)
		{
			if (hasReach)
			{
				if (eatDuration >= 0f)
				{
					return null;
				}
				return GetState<IdleState>();
			}
			if (Mathf.Abs(animal.transform.position.x - currentDestination) < 0.5f)
			{
				hasReach = true;
				eatDuration = 2f;
				PlayAnimation("eat", force: true);
			}
			return null;
		}

		public override void OnEnter()
		{
			hasReach = false;
			animal.PlayAnimation("move");
			currentDestination = animal.EatPoint.x;
			float f = currentDestination - animal.transform.position.x;
			float dst = Mathf.Abs(f);
			float moveSpeed = animal.GetMoveSpeed(dst);
			moveDelta = new Vector2(Mathf.Sign(f) * moveSpeed, 0f);
			animal.transform.localScale = new Vector3(0f - Mathf.Sign(f), 1f, 1f);
		}

		protected override void OnFixedUpdate(float dt)
		{
			if (hasReach)
			{
				eatDuration -= dt;
			}
			else
			{
				animal.transform.Translate(moveDelta * dt);
			}
		}
	}

	private Animator animator;

	private SpriteRenderer spriteRenderer;

	private AnimalInfo animalProto;

	private Vector2 startPoint;

	private Vector2 endPoint;

	private Vector2 transitionPoint;

	private bool isChild;

	private RedSaw.AI.StateMachine.StateMachine _stateMachine;

	private Vector2 colliderSize;

	private bool hasFondled;

	private Vector2[] feederPoints;

	private int index;

	public Vector2 PositionTip
	{
		get
		{
			Vector3 vector = base.transform.position;
			vector.y += colliderSize.y + 1.5f;
			return vector;
		}
	}

	private Vector2 RandomPoint => new Vector2(Random.Range(startPoint.x, endPoint.x), startPoint.y);

	public Vector2 EatPoint { get; set; }

	private float GetMoveSpeed(float dst)
	{
		if (isChild)
		{
			if (!(Random.value > 0.5f))
			{
				return animalProto.RunSpeed;
			}
			return animalProto.MoveSpeed;
		}
		return animalProto.MoveSpeed;
	}

	private void Face(Vector2 positionWS)
	{
		float f = positionWS.x - base.transform.position.x;
		base.transform.localScale = new Vector3(0f - Mathf.Sign(f), 1f, 1f);
	}

	public void ShowAnimal()
	{
		position2d = transitionPoint;
		base.transform.position += new Vector3(0f, 0f, (float)index * -1E-05f);
	}

	public void HideAnimal()
	{
		position2d = new Vector2(9999f, 9999f);
	}

	public void Setup(string animalName, Vector2 startPoint, float activeRange, Vector2 transitionPoint, bool isChild, Vector2[] feederPoints, int index)
	{
		if (DolocConfig.Tables.TbAnimal.DataMap.TryGetValue(animalName, out animalProto))
		{
			this.startPoint = startPoint;
			endPoint = new Vector2(startPoint.x + activeRange, startPoint.y);
			this.transitionPoint = transitionPoint;
			this.feederPoints = feederPoints;
			this.index = index;
			_Setup(animalProto, isChild);
		}
	}

	private void _Setup(AnimalInfo proto, bool isChild)
	{
		if (proto != null)
		{
			animator.runtimeAnimatorController = (isChild ? proto.ChildAnimator.Asset : proto.Animator.Asset);
			position2d = transitionPoint;
			base.transform.position += new Vector3(0f, 0f, (float)index * -1E-05f);
			BoxCollider2D component = GetComponent<BoxCollider2D>();
			colliderSize = (isChild ? proto.ColliderSizeChild : proto.ColliderSizeAdult);
			component.size = colliderSize;
			component.offset = new Vector2(0f, colliderSize.y * 0.5f);
			PlayAnimation("idle", force: true);
			if (GetShouldQuit())
			{
				_stateMachine.ChangeState<HideState>();
				return;
			}
			_stateMachine.ChangeState<IdleState>();
			position2d = RandomPoint;
		}
	}

	private bool TryGetRandomFeederPoint(out Vector2 point)
	{
		point = default(Vector2);
		if (feederPoints.IsNullOrEmpty())
		{
			return false;
		}
		point = feederPoints.Choice();
		return true;
	}

	private bool TrySetFeederPoint()
	{
		if (TryGetRandomFeederPoint(out var point))
		{
			EatPoint = point;
			return true;
		}
		return false;
	}

	private void RaiseEmotion(EmotionName name)
	{
		DolocAPI.RaiseEmotionLimited(base.transform, name);
	}

	private void PlayAnimation(string name, bool force = false)
	{
		if (!(animator == null))
		{
			if (force)
			{
				animator.Play(name);
			}
			else if (!animator.GetCurrentAnimatorStateInfo(0).IsName(name))
			{
				animator.Play(name);
			}
		}
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(() => PositionTip, DolocConfig.StaticTexts.UiOperationFondle);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		DolocAPI.agent._Interact(delegate
		{
			Face(DolocAPI.AgentPosition);
			_stateMachine.ChangeState(delegate(IdleState state)
			{
				state.SetWaitDuration(3f);
			});
			RaiseEmotion(EmotionName.LOVE);
		});
	}

	private void Update()
	{
		_stateMachine.Update(Time.deltaTime);
	}

	private void FixedUpdate()
	{
		_stateMachine.FixedUpdate(Time.fixedDeltaTime);
	}

	public void OnRecycle()
	{
	}

	public void OnCreated()
	{
		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		_stateMachine = new RedSaw.AI.StateMachine.StateMachine("decorative_animal", new object[1] { this });
	}

	public void OnReuse()
	{
	}

	public static bool GetShouldQuit()
	{
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		if (currentWeatherType.IsRainyWeather() || currentWeatherType == WeatherType.ACID_RAIN)
		{
			return true;
		}
		return DolocAPI.archiveHandle.CurrentDayPeriodType == DayPeriodType.Night;
	}

	private void ResetState()
	{
		_stateMachine.ChangeState<IdleState>();
	}
}
