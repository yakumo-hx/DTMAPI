using System;
using DolocTown.Config.Item;
using DolocTown.GameData;
using DolocTown.UI;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

[GameEntityManager("/farm/animal", DolocGameAssets.GAME_ENTITY_ANIMAL, CustomManagement = true)]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class AnimalRenderer : GameRendererEntity, IFellable, IInteractable
{
	private Animator _animator;

	private Vector2 _statusOffset;

	private readonly GameEntitySlotUI<AnimalStatusTip> statusTipHandle = new GameEntitySlotUI<AnimalStatusTip>();

	private bool _showMetabolismFlag;

	private bool _showFondleFlag;

	private bool _isMoving;

	private float _movingTimer;

	private Vector2 _movingDirection;

	private Action _moveCallback;

	private bool _isJumping;

	private bool _isJumpingReady;

	private bool _isEating;

	public Animal animal { get; set; }

	public SpriteRenderer _spriteRenderer { get; protected set; }

	public override SpriteRenderer outlineTarget => _spriteRenderer;

	public Sprite Sprite
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

	public Vector2 EmotionOffset
	{
		set
		{
			_statusOffset = value + new Vector2(-0.25f, -0.25f);
			EmotionPositionControl component = GetComponent<EmotionPositionControl>();
			if (component != null)
			{
				component.EmotionOffset = value;
			}
			ParticleSystem componentInChildren = GetComponentInChildren<ParticleSystem>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.transform.localPosition = value;
			}
		}
	}

	public bool ShowMetabolismFlag
	{
		get
		{
			return _showMetabolismFlag;
		}
		set
		{
			if (_showMetabolismFlag != value)
			{
				_showMetabolismFlag = value;
				_RefreshStatusTip();
			}
		}
	}

	public bool ShowFondleFlag
	{
		set
		{
			if (_showFondleFlag != value)
			{
				_showFondleFlag = value;
				_RefreshStatusTip();
			}
		}
	}

	public bool ShowSleepEffects
	{
		set
		{
			ParticleSystem componentInChildren = GetComponentInChildren<ParticleSystem>(includeInactive: true);
			if (componentInChildren == null)
			{
				return;
			}
			if (value)
			{
				if (!componentInChildren.gameObject.activeSelf)
				{
					componentInChildren.gameObject.SetActive(value: true);
				}
				componentInChildren.Play();
			}
			else
			{
				if (componentInChildren.isPlaying)
				{
					componentInChildren.Stop();
				}
				componentInChildren.gameObject.SetActive(value: false);
			}
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
			if (value)
			{
				base.transform.localScale = new Vector3(-1f, 1f, 1f);
			}
			else
			{
				base.transform.localScale = new Vector3(1f, 1f, 1f);
			}
		}
	}

	public Vector2 BoxColliderSize
	{
		set
		{
			BoxCollider2D component = GetComponent<BoxCollider2D>();
			if (!(component == null))
			{
				component.size = value;
				component.offset = new Vector2(0f, value.y / 2f);
			}
		}
	}

	public RuntimeAnimatorController animatorController
	{
		get
		{
			return _animator.runtimeAnimatorController;
		}
		set
		{
			_animator.runtimeAnimatorController = value;
		}
	}

	public bool IsJumping => _isJumping;

	public bool ShouldCostEnergy => false;

	public bool ShouldCostChopCounter => false;

	public Vector2 PositionTip => new Vector2(base.transform.position.x, base.transform.position.y + 5.5f);

	public bool OnlyTouch => false;

	public bool CanInteractContinues => false;

	public int Priority => animal?.InteractPriority ?? 0;

	protected override void __Init()
	{
		base.__Init();
		_spriteRenderer = GetComponent<SpriteRenderer>();
		_animator = GetComponent<Animator>();
	}

	public override void PostSoundEventByEnum(SoundEvents soundEvent)
	{
		if (DolocAPI.userInput.CheckState<NormalGameState>())
		{
			base.PostSoundEventByEnum(soundEvent);
		}
	}

	public override void PostSoundEvent(string soundEvent)
	{
		if (DolocAPI.userInput.CheckState<NormalGameState>())
		{
			base.PostSoundEvent(soundEvent);
		}
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		_moveCallback = null;
		_isMoving = false;
		_isEating = false;
		_isJumping = false;
		ShowMetabolismFlag = false;
		ShowFondleFlag = false;
		ShowSleepEffects = false;
		statusTipHandle.Release();
	}

	public override void OnReuse()
	{
		base.OnReuse();
		_spriteRenderer.sortingOrder = 0;
	}

	private void FixedUpdate()
	{
		if (animal != null)
		{
			if (_isMoving)
			{
				_UpdateMove();
			}
			else if (_isJumping)
			{
				_UpdateJump();
			}
			else if (_isEating)
			{
				_UpdateEat();
			}
		}
	}

	private void _RefreshStatusTip()
	{
		statusTipHandle.Do(delegate(AnimalStatusTip R)
		{
			R.PositionGetter = () => (Vector2)base.transform.position + _statusOffset;
			if (_showMetabolismFlag && _showFondleFlag)
			{
				R.Render(LocSprites.UI_TIP_ANIMAL_COLLECT, LocSprites.UI_TIP_ANIMAL_FONDLE);
			}
			else if (_showMetabolismFlag)
			{
				R.Render(LocSprites.UI_TIP_ANIMAL_COLLECT, null);
			}
			else if (_showFondleFlag)
			{
				R.Render(LocSprites.UI_TIP_ANIMAL_FONDLE, null);
			}
			else
			{
				R.Render(null, null);
			}
		});
	}

	private void _UpdateMove()
	{
		if (_isJumping)
		{
			_spriteRenderer.sortingOrder = 0;
			_isJumping = false;
		}
		_movingTimer -= Time.fixedDeltaTime;
		if (_movingTimer <= 0f)
		{
			_moveCallback?.Invoke();
			_moveCallback = null;
			_isMoving = false;
		}
		base.transform.Translate(_movingDirection * Time.fixedDeltaTime);
	}

	private void _UpdateJump()
	{
		if (_isJumpingReady)
		{
			bool isDone;
			Vector2 vector = animal.Jumper.Update(Time.fixedDeltaTime, out isDone);
			position2d = vector;
			if (isDone)
			{
				_animator.Play("idle");
			}
		}
		else if (IsAnimationDone("jump_ready"))
		{
			_isJumpingReady = true;
			_animator.Play("jump");
		}
	}

	private void _UpdateEat()
	{
		if (IsAnimationDone("eat"))
		{
			PlayAnimation("idle");
			_isEating = false;
		}
	}

	public void Jump(Vector2 dest)
	{
		_isJumping = true;
		_isJumpingReady = false;
		_spriteRenderer.sortingOrder = 1;
		PlayAnimation("jump_ready");
		float f = dest.x - base.transform.position.x;
		base.transform.localScale = new Vector3(0f - Mathf.Sign(f), 1f, 1f);
		PostSoundEventByEnum(SoundEvents.PLAY_ANIMAL_JUMP);
	}

	public void StopJump()
	{
		_isJumping = false;
		_animator.Play("idle");
		_spriteRenderer.sortingOrder = 0;
		position2d = animal.Jumper.destination;
		DolocAPI.RaiseInstantAnimEffects(base.transform.position, InstAnimEffectType.PLAYER_LAND_SMOKE);
		PostSoundEventByEnum(SoundEvents.PLAY_ANIMAL_DROP);
	}

	public void MoveTo(Vector2 dest)
	{
		_isMoving = true;
		_movingTimer = 1f;
		_moveCallback = null;
		float f = dest.x - base.transform.position.x;
		float num = Mathf.Abs(f);
		float num2 = Mathf.Sign(f);
		_movingDirection = new Vector2(num2 * num, 0f);
		base.transform.localScale = new Vector3(0f - num2, 1f, 1f);
	}

	public void MoveTo(Vector2 dest, float moveSpeed, Action callback = null)
	{
		_isMoving = true;
		_moveCallback = callback;
		moveSpeed = Mathf.Clamp(moveSpeed, 1f, 10f);
		float f = dest.x - base.transform.position.x;
		float num = Mathf.Abs(f);
		_movingTimer = num / moveSpeed;
		_movingDirection = new Vector2(Mathf.Sign(f) * moveSpeed, 0f);
		base.transform.localScale = new Vector3(0f - Mathf.Sign(f), 1f, 1f);
	}

	public void StopMove()
	{
		if (_isMoving)
		{
			_isMoving = false;
			_movingTimer = 0f;
			PlayAnimation("idle");
		}
	}

	public void Eat(bool playAnimation = true)
	{
		if (playAnimation)
		{
			PlayAnimation("eat");
		}
		_isEating = true;
	}

	public void PlayAnimation(string animName, bool force = false, float normalizedTime = 0f)
	{
		if (force)
		{
			_animator.Play(animName, 0, normalizedTime);
		}
		else if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(animName))
		{
			_animator.Play(animName, 0, normalizedTime);
		}
	}

	private bool IsAnimationDone(string name, float normalizedTime = 1f)
	{
		AnimatorStateInfo currentAnimatorStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.IsName(name))
		{
			return currentAnimatorStateInfo.normalizedTime >= normalizedTime;
		}
		return false;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		other.GetComponent<IAnimalTouchable>()?.OnAnimalTouch();
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		other.GetComponent<IAnimalTouchable>()?.OnAnimalDistouch();
	}

	public bool OnFell(ItemTool tool, Vector2 hitPosition)
	{
		if (animal == null)
		{
			Debug.LogError("<color=red>严重BUG：小动物未绑定数据实体</color>");
			return false;
		}
		if (animal.isSleep)
		{
			animal.WakeUp();
			animal.Face(DolocAPI.AgentPosition);
			DolocAPI.RaiseEmotion(base.transform, EmotionName.CONFUSE);
			animal.controller.ChangeTask(LinearTask.WaitFrames(3));
			return false;
		}
		if (_HandleAnimalProduct(tool, hitPosition))
		{
			return false;
		}
		animal.PlayAnimalSound();
		DolocAPI.RaiseInstantAnimEffects(hitPosition, InstAnimEffectType.IMPACT_01);
		if (animal.Flee())
		{
			DolocAPI.RaiseEmotion(base.transform, EmotionName.SCARED);
		}
		return false;
	}

	private bool _HandleAnimalProduct(ItemTool tool, Vector2 hitPosition)
	{
		if (tool.ToolType != ToolType.SICKLE)
		{
			return false;
		}
		if (animal.protoName != "goat" || !animal.NeedMetabolism)
		{
			return false;
		}
		DolocAPI.RaiseInstantPSEffects(hitPosition, InstantParticleEffectsType.BRUST_STARS);
		animal.Produce();
		return true;
	}

	public void OnTouch()
	{
		DolocAPI.CurrentAnimal = animal;
		base.ShowOutline = true;
		this.ShowSceneOperationTip(() => PositionTip, animal.Title, DolocAPI.UserInput.GlobalInteractActionName);
	}

	public void OnInteract()
	{
		if (animal.Invoke())
		{
			StopMove();
			FaceRight = position.x < DolocAPI.AgentPosition.x;
		}
		_HandleInteract();
	}

	private void _HandleInteract()
	{
		this.PushSceneOperationTipToHide();
		DolocAPI.uiSystem.sceneBoxGroup.RenderAndShow(base.transform.position.x, animal, DolocAPI.GlobalParameter.UiSceneInfoTipDuration);
		DolocAPI.agent._Interact(delegate
		{
			animal.Fondle();
			DolocAPI.uiSystem.sceneBoxGroup.TryUpdateInfo();
		});
	}

	public void OnDisTouch()
	{
		if (DolocAPI.CurrentAnimal == animal)
		{
			DolocAPI.CurrentAnimal = null;
		}
		base.ShowOutline = false;
		this.HideSceneOperationTip();
	}
}
