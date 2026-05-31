using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Player;
using DolocTown.Config.Tile;
using DolocTown.GameData;
using DolocTown.UI;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
public class BodyController : DolocObject, IAttackable, IBombInteractive
{
	public enum PlayerAudioType
	{
		NONE,
		FOOTSTEP,
		USE_TOOL,
		YAWN,
		FISHING_THROW_HOOK
	}

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private Rigidbody2D rigidbody2d;

	[SerializeField]
	private BoxCollider2D bodyCollider;

	[SerializeField]
	private Transform _weaponPosition;

	[SerializeField]
	private SpriteRenderer _spriteRenderer;

	[SerializeField]
	private AgentLight weakLight;

	[SerializeField]
	private GroundChecker groundChecker;

	[SerializeField]
	private EnvironmentChecker wallChecker;

	[SerializeField]
	public ToolRenderer ToolRenderer;

	[SerializeField]
	public FishRodRenderer fishRodRenderer;

	[SerializeField]
	private EnvironmentChecker wallTopChecker;

	[SerializeField]
	private Transform hiddenTrigger;

	[SerializeField]
	private float maxDropSpeed = -20f;

	[SerializeField]
	private bool hasGhostShadow = true;

	public bool debugStateChangeLog;

	private AgentHatRenderer defaultHatRenderer;

	private DolocUserInput userInput;

	private Action waterCallback;

	private Action standUpCallback;

	private Action<bool> throwCallback;

	private bool _pause;

	private bool _disableUpdate;

	private bool _inCutscene;

	private readonly RSTimer _dashCdTimer = new RSTimer();

	private ProgressCircle _dashCdProgressCircle;

	private Tween _dashCdAnimation;

	private bool _isDashCdNow;

	private bool _dashLimited;

	private Vector2 _velocityCache;

	private bool isForegroundInDialogue;

	protected AttackableType _attackableType = AttackableType.Player;

	public float DefaultZ { get; private set; }

	public AgentHatRenderer HatRenderer { get; private set; }

	public PlayerSplitRenderer SplitRenderer { get; private set; }

	public AgentStateManager StateManager { get; private set; }

	public AgentPhysicalStatus Status { get; private set; }

	public FishingCache FishingCache { get; private set; } = new FishingCache();


	private DismantleChecker DismantleChecker { get; set; }

	public MotionAbility MotionAbility => DolocAPI.AbilitySystem.motionAbility;

	public Transform DroneFollower => _weaponPosition;

	public Vector2 PositionCenter => bodyCollider.bounds.center;

	public Vector2 PositionHeadTop => PositionCenter + new Vector2(0f, 3.5f);

	public Vector2 PositionThrow => (Vector2)base.transform.position + new Vector2(IsFaceRight ? 1.275f : (-1.275f), 1.275f);

	public Vector2 Velocity => Status.Velocity;

	public Room CurrentRoom => DolocAPI.archiveHandle.currentRoom;

	public DolocUserInput UserInput => userInput;

	public bool _isWaitingForJumpDownReleased { get; private set; }

	public bool ShouldClearPhysicalStatusWhileTransit
	{
		get
		{
			AgentStateBase current = StateManager.current;
			if (!(current is AgentStateDash) && !(current is AgentStateDrop))
			{
				return !(current is AgentStateJump);
			}
			return false;
		}
	}

	public bool Pause
	{
		get
		{
			return _pause;
		}
		set
		{
			_pause = value;
			bool flag = !_pause;
			animator.enabled = flag;
			HatRenderer.PauseAnimator = value;
			ToolRenderer.PauseAnimator = value;
			if (_pause)
			{
				_velocityCache = Velocity;
			}
			rigidbody2d.bodyType = (_pause ? RigidbodyType2D.Static : RigidbodyType2D.Dynamic);
			if (flag)
			{
				rigidbody2d.velocity = _velocityCache;
			}
			hiddenTrigger.SetParent(value ? null : base.transform);
			hiddenTrigger.position = base.transform.position;
		}
	}

	public bool InCutscene
	{
		get
		{
			return _inCutscene;
		}
		set
		{
			_inCutscene = value;
			rigidbody2d.isKinematic = value;
			if (value)
			{
				DolocAPI.agent.StateManager.Overwrite<AgentStateIdle>();
			}
			_disableUpdate = value;
			ResetVelocity();
		}
	}

	public bool EnableRbGravity
	{
		set
		{
			rigidbody2d.gravityScale = (value ? MotionAbility.GravityScale : 0f);
		}
	}

	public bool EnableBodyCollider
	{
		set
		{
			bodyCollider.enabled = value;
		}
	}

	public bool WeakLightEnabled
	{
		set
		{
			if (value)
			{
				weakLight.Show();
			}
			else
			{
				weakLight.Hide();
			}
		}
	}

	public bool IsDash => StateManager.CheckState<AgentStateDash>();

	public bool IsFaint { get; private set; }

	public bool IsInAirState
	{
		get
		{
			AgentStateBase current = StateManager.current;
			return current is AgentStateJump || current is AgentStateDrop || current is AgentStateDash;
		}
	}

	public bool IsCurrentStateSupportUseItem => StateManager.current.SupportUseItem;

	public bool IsCurrentStateSupportInteract => StateManager.current.SupportInteract;

	public bool IsCurrentStateSupportScrollQuickInventoryUI => StateManager.current.SupportScrollQuickInventoryUI;

	public bool IsCurrentStateSupportTeleport
	{
		get
		{
			if (DolocAPI.IsAgentRiding)
			{
				return true;
			}
			return !StateManager.current.Check<AgentStateTool>();
		}
	}

	public bool IsFaceRight
	{
		get
		{
			return base.transform.localScale.x > 0f;
		}
		set
		{
			base.transform.localScale = (value ? new Vector3(1f, 1f, 1f) : new Vector3(-1f, 1f, 1f));
		}
	}

	public HatInfo CurrentHatRenderInfo => HatRenderer.CurrentHatRenderInfo;

	public int SortingOrder
	{
		set
		{
			_spriteRenderer.sortingOrder = value;
			HatRenderer.SortingOrder = value;
			SplitRenderer.SortingOrder = value;
		}
	}

	public string SortingLayerName
	{
		set
		{
			_spriteRenderer.sortingLayerName = value;
			HatRenderer.SortingLayerName = value;
			SplitRenderer.SortingLayerName = value;
		}
	}

	public bool IsClimb
	{
		get
		{
			if (base.transform.localScale.x > 0f)
			{
				return userInput.NormalMoveFactor > 0f;
			}
			return userInput.NormalMoveFactor < 0f;
		}
	}

	public bool ShouldEnterToolState
	{
		get
		{
			if (userInput.NormalUseTool || userInput.NormalUseToolInProgress)
			{
				if (DolocAPI.HasEnoughEnergyForUsingTool() && !DolocAPI.userInput.CurrentState.DisableUseItem)
				{
					return DolocAPI.SelectedItem is ItemTool;
				}
				return false;
			}
			return false;
		}
	}

	public bool IsFishingNow => StateManager.current.Check((AgentStateFishingWait s) => s.IsWaitNow);

	public AttackableType attackableType => _attackableType;

	public bool IsPerfectDodge => StateManager.CheckState<AgentStateDash>();

	[DebugInfo("移动速度", Color = "#00ff00")]
	public float MoveSpeed => MotionAbility.MoveSpeed + DolocAPI.AgentEquipmentParams.moveSpeedAddition;

	[DebugInfo("冲刺冷却时间", Color = "#00ffff")]
	public float DashCdDuration => Mathf.Max(0.1f, DolocAPI.GlobalParameter.DashCdDuration - DolocAPI.AgentEquipmentParams.dashCdDecrease);

	public int CurrentDefend => DolocAPI.AbilitySystem.battleAbility.GetDefend(DolocAPI.GlobalParameter.AgentDefend + DolocAPI.AgentEquipmentParams.defence);

	public void ResetVelocity()
	{
		Status.Velocity = Vector2.zero;
	}

	public void SetVelocity(Vector2 velocity)
	{
		Status.Velocity = velocity;
	}

	public void ResetCollider()
	{
		bodyCollider.enabled = false;
		bodyCollider.enabled = true;
	}

	public void SetToForeground(bool active)
	{
		isForegroundInDialogue = active;
		DolocAPI.AgentRenderer?.SetToForeground(active && (CurrentRoom?.HasResource ?? false));
	}

	public void RefreshSortingLayerInRoom()
	{
		SetToForeground(isForegroundInDialogue);
	}

	public void Init(DolocUserInput userInput)
	{
		DefaultZ = base.transform.position.z;
		this.userInput = userInput;
		SplitRenderer = GetComponentInChildren<PlayerSplitRenderer>(includeInactive: true);
		SplitRenderer.Init();
		defaultHatRenderer = GetComponentInChildren<AgentHatRenderer>(includeInactive: true);
		defaultHatRenderer.InitHat();
		HatRenderer = defaultHatRenderer;
		ToolRenderer.Init();
		fishRodRenderer.Init();
		weakLight.Init();
		DismantleChecker = new DismantleChecker();
		EnableRbGravity = true;
		Status = new AgentPhysicalStatus(rigidbody2d, groundChecker, wallChecker, wallTopChecker, maxDropSpeed);
		StateManager = new AgentStateManager(this);
	}

	public void PlayAnimation(string name, float normalizedTime = 0f)
	{
		animator.Play(name, 0, normalizedTime);
		HatRenderer.Play(name, normalizedTime);
		SplitRenderer.Play(name);
	}

	public void SetAnimatorUpdateUnscaled(bool value)
	{
		animator.updateMode = (value ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal);
		HatRenderer.SetAnimatorUpdateUnscaled(value);
	}

	public void SetHatInfo(HatInfo info = null)
	{
		if (info == null)
		{
			HatRenderer.CurrentHatRenderInfo = null;
			SplitRenderer.Play("idle");
			return;
		}
		if (AgentHatRendererUtils.TryLoadHatRenderer(info.Id, isRiding: false, out var hatRenderer))
		{
			defaultHatRenderer.SetVisible(value: false);
			HatRenderer?.SetVisible(value: false);
			hatRenderer.transform.SetParent(base.transform);
			hatRenderer.transform.localScale = Vector3.one;
			hatRenderer.transform.localPosition = new Vector3(0f, 0f, -0.01f);
			hatRenderer.CurrentHatRenderInfo = info;
			hatRenderer.OnRender();
			HatRenderer = hatRenderer;
		}
		else
		{
			if (HatRenderer != defaultHatRenderer)
			{
				HatRenderer.SetVisible(value: false);
			}
			HatRenderer = defaultHatRenderer;
			HatRenderer.CurrentHatRenderInfo = info;
			HatRenderer.SetVisible(value: true);
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		AnimatorClipInfo[] currentAnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(0);
		if (currentAnimatorClipInfo.Length != 0)
		{
			string animName = currentAnimatorClipInfo[0].clip.name;
			HatRenderer.Play(animName, currentAnimatorStateInfo.normalizedTime);
			SplitRenderer.Play(animName);
		}
	}

	public bool IsAnimationDone(string name, out float process)
	{
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		process = currentAnimatorStateInfo.normalizedTime;
		if (currentAnimatorStateInfo.IsName(name))
		{
			return currentAnimatorStateInfo.normalizedTime >= 1f;
		}
		return false;
	}

	public float GetAnimationNormalizedTime()
	{
		return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
	}

	public void SetRoom(Room room, bool isRiding)
	{
		groundChecker.Reset();
		if (room != null)
		{
			ToolRenderer.SetVisible(value: false);
			if (!isRiding && room.Type != RoomType.Dungeon)
			{
				StateManager.Overwrite<AgentStateIdle>();
			}
			StateManager.UnsetFixed();
			AllowDash(animated: true);
			if (rigidbody2d.bodyType != 0 && !InCutscene)
			{
				rigidbody2d.bodyType = RigidbodyType2D.Dynamic;
			}
			if (room.Type != RoomType.Dungeon)
			{
				StateManager.GetState<AgentStateJump>().jumpTimes = DolocAPI.archiveHandle.DoubleJumpTimes();
			}
		}
	}

	public bool Dash(Vector2 dir)
	{
		if (!StateManager.current.SupportDash)
		{
			return false;
		}
		if (_isDashCdNow || _dashLimited)
		{
			return false;
		}
		AgentStateDash state = StateManager.GetState<AgentStateDash>();
		state.dashDir = dir * MotionAbility.DashSpeed;
		StateManager.Overwrite(state);
		return true;
	}

	public void GhostShadow()
	{
		if (hasGhostShadow)
		{
			if (SplitRenderer.BaseSprite != null)
			{
				DolocAPI.RaiseGhostShadow(base.transform.position, base.transform.localScale, SplitRenderer.BaseSprite);
			}
			if (SplitRenderer.BodySprite != null)
			{
				DolocAPI.RaiseGhostShadow(base.transform.position, base.transform.localScale, SplitRenderer.BodySprite);
			}
			if (SplitRenderer.HairSprite != null)
			{
				DolocAPI.RaiseGhostShadow(base.transform.position - new Vector3(0f, 0f, 0.0001f), base.transform.localScale, SplitRenderer.HairSprite);
			}
			if (HatRenderer.CurrentSprite != null)
			{
				DolocAPI.RaiseGhostShadow(base.transform.position - new Vector3(0f, 0f, 0.0002f), base.transform.localScale, HatRenderer.CurrentSprite);
			}
		}
	}

	public bool Jump()
	{
		if (StateManager.current.Check<AgentStateClimb>())
		{
			StateManager.Overwrite<AgentStateClimbJump>();
			return true;
		}
		if (StateManager.current.SupportJump)
		{
			CheckJumpDownReleased(isKeyReleased: true);
			StateManager.Overwrite<AgentStateJump>();
			return true;
		}
		return false;
	}

	public void SuperJump()
	{
		if (!IsFaint && StateManager.current.SupportJump)
		{
			StateManager.Overwrite<AgentStateJumpTrampoline>();
		}
	}

	public void CheckJumpDownReleased(bool isKeyReleased)
	{
		if (_isWaitingForJumpDownReleased && isKeyReleased)
		{
			_isWaitingForJumpDownReleased = false;
			base.gameObject.layer = LayerMask.NameToLayer("Player");
		}
	}

	public bool JumpDown()
	{
		if (StateManager.CheckState<AgentStateDrop>() || StateManager.CheckState<AgentStateJump>() || StateManager.CheckState<AgentStateClimbJump>() || StateManager.CheckState<AgentStateJumpTrampoline>())
		{
			if (!_isWaitingForJumpDownReleased)
			{
				_isWaitingForJumpDownReleased = true;
				base.gameObject.layer = LayerMask.NameToLayer("PlatformExclude");
			}
			return true;
		}
		if (!IsCurrentStateSupportInteract)
		{
			return false;
		}
		if (groundChecker.PlatformCount <= 0)
		{
			return false;
		}
		_isWaitingForJumpDownReleased = false;
		base.gameObject.layer = LayerMask.NameToLayer("PlatformExclude");
		UniTask.Delay(200, ignoreTimeScale: false, PlayerLoopTiming.FixedUpdate).ContinueWith(delegate
		{
			if (DolocAPI.UserInput.NormalJumpDownInProgress)
			{
				_isWaitingForJumpDownReleased = true;
			}
			else
			{
				base.gameObject.layer = LayerMask.NameToLayer("Player");
			}
		}).Forget();
		DolocAPI.Broadcast(OperationEventType.JUMP_DOWN_PLATFORM);
		return true;
	}

	public void Drop()
	{
		if (StateManager.current.Check<AgentStateClimb>())
		{
			StateManager.Overwrite<AgentStateDrop>();
		}
	}

	public void UseTool(ItemTool tool)
	{
		if (DolocAPI.IsAgentRiding)
		{
			return;
		}
		if (StateManager.current is AgentStateTool agentStateTool)
		{
			if (agentStateTool.animationNormalizedTime < 0.95f)
			{
				return;
			}
			if (!DolocAPI.HasEnoughEnergyForUsingTool())
			{
				if (ToolRenderer.HasCachedResourceRealtime)
				{
					StateManager.Overwrite<AgentStateTool>(shouldQuit: false);
				}
				else
				{
					DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrLackOfEnergy);
				}
			}
			else
			{
				StateManager.Overwrite<AgentStateTool>(shouldQuit: false);
			}
			return;
		}
		ToolRenderer.ClearResourceCache(force: true);
		if (!DolocAPI.HasEnoughEnergyForUsingTool())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrLackOfEnergy);
			return;
		}
		StateManager.Overwrite(delegate(AgentStateTool state)
		{
			state.tool = tool;
		});
	}

	public void _Interact(Action callback = null, string animName = "interact")
	{
		Status.Velocity = Vector2.zero;
		StateManager.Overwrite(delegate(AgentStateInteract state)
		{
			state.SetStatus(callback, animName);
		});
	}

	public void _Throw(Action<bool> callback = null)
	{
		throwCallback = callback;
		StateManager.Overwrite(delegate(AgentStateInteract state)
		{
			state.SetStatus(null, "throw", supportJump: false, supportDash: false);
		});
	}

	private void _ThrowEvent()
	{
		throwCallback?.Invoke(IsFaceRight);
		throwCallback = null;
	}

	public void _Water(ItemWaterCan waterCan, Action callback = null)
	{
		waterCallback = callback;
		DolocAPI.CostToolEnergy();
		StateManager.Overwrite(delegate(AgentStateWater state)
		{
			state.waterCan = waterCan;
		});
	}

	public void Faint(FaintReason reason, bool shouldMarkFaintState = true)
	{
		if (shouldMarkFaintState)
		{
			IsFaint = true;
		}
		if (AgentStateFishing.IsUiControlled)
		{
			AgentStateFishing.UnsetUiControl();
		}
		DolocAPI.BroadcastString(GameEventType.FAINT, reason.ToString().ToLower());
		StateManager.Overwrite<AgentStateFaint>();
		MotionAbility.ClearEnvModerate();
	}

	public void UnsetFaint()
	{
		IsFaint = false;
	}

	public void Stand(Action callback = null)
	{
		standUpCallback = callback;
		StateManager.Overwrite<AgentStateStand>();
	}

	public void OnStandDone()
	{
		standUpCallback.InvokeSafe();
		standUpCallback = null;
	}

	public void EnterState<T>(bool shouldQuit = true) where T : AgentStateBase
	{
		StateManager.Overwrite<T>(shouldQuit);
	}

	public bool CheckState<T>() where T : AgentStateBase
	{
		return StateManager.CheckState<T>();
	}

	public void LoadCurrentTool()
	{
		if (DolocAPI.SelectedItem is ItemTool tool)
		{
			StateManager.GetState<AgentStateTool>().tool = tool;
		}
	}

	public void StartDashCD()
	{
		_isDashCdNow = true;
		_dashCdTimer.SetInterval(DashCdDuration);
	}

	public void ResetDashCD(bool animated)
	{
		_isDashCdNow = false;
		if (animated && !_dashLimited)
		{
			ShowDashRecoveryAnimation();
		}
	}

	public void LimitDash()
	{
		_dashLimited = true;
	}

	public void AllowDash(bool animated)
	{
		if (_dashLimited)
		{
			_dashLimited = false;
			if (animated && !_isDashCdNow)
			{
				ShowDashRecoveryAnimation();
			}
		}
	}

	private void ShowDashRecoveryAnimation()
	{
		DolocAPI.RaiseInstantPSEffects(position, InstantParticleEffectsType.LIGHT_SPARKS);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_CONFIRM);
	}

	public void PostAudioEvent(PlayerAudioType audioType)
	{
		switch (audioType)
		{
		case PlayerAudioType.FOOTSTEP:
		{
			TileMaterial material;
			if (DolocAPI.IsAgentInWater)
			{
				material = TileMaterial.UNDERWATER;
				DolocAPI.CurrentWater.OnWalkInWater();
			}
			else if (!CurrentRoom.GetTileMaterial(bodyCollider.transform.position, out material))
			{
				Vector3 vector = bodyCollider.transform.position;
				vector.y += 0.1f;
				RaycastHit2D raycastHit2D = Physics2D.Raycast(vector, Vector2.down, DolocAPI.GlobalParameter.BuilderRaycastDistance, DolocAPI.gameConfig.groundMask);
				if (raycastHit2D.collider != null && raycastHit2D.collider.gameObject.GetComponent<InteractableObject>() is ITileMaterial tileMaterial)
				{
					material = tileMaterial.tileMaterial;
				}
			}
			if (DolocConfig.Tables.TbMaterialSound.DataMap.TryGetValue(material, out var value))
			{
				DolocAPI.Sound.SetSoundSwitch(value.GroupName, value.StateName, base.gameObject);
			}
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_FOOTSTEP);
			break;
		}
		case PlayerAudioType.USE_TOOL:
			if (StateManager.current is AgentStateTool agentStateTool)
			{
				DolocAPI.Sound.PostSoundEvent(((ItemFunctionTool)agentStateTool.tool.proto.Function).AudioEvent);
			}
			break;
		case PlayerAudioType.YAWN:
			if (DolocAPI.userInput.CurrentState is NormalGameState)
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_YAWN);
			}
			break;
		case PlayerAudioType.FISHING_THROW_HOOK:
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_FISHING_THROW_HOOK);
			break;
		}
	}

	private void _HitEvent()
	{
		StateManager.Overwrite<AgentStateIdle>();
	}

	public void KnockEvent()
	{
		DismantleChecker.Check();
	}

	private void _WaterEvent()
	{
		waterCallback?.Invoke();
		waterCallback = null;
	}

	public void UpdatePerTU()
	{
		HatRenderer.UpdatePerTU();
	}

	public void Update()
	{
		if (!_pause && !_disableUpdate)
		{
			StateManager.current.Update();
			if (_dashCdProgressCircle != null && _dashCdProgressCircle.enabled)
			{
				_dashCdProgressCircle.position = DolocAPI.WorldToScreen(PositionHeadTop);
			}
		}
	}

	public void FixedUpdate()
	{
		if (!_pause && !_disableUpdate)
		{
			StateManager.current.OnPlay();
			if (_isDashCdNow && _dashCdTimer.Tick(Time.fixedDeltaTime))
			{
				ResetDashCD(animated: true);
			}
		}
	}

	private void OnEnable()
	{
		StateManager.Overwrite<AgentStateIdle>(shouldQuit: false);
		DolocAPI.Sound.RegisterSpatialAudioListener(base.gameObject);
	}

	private void OnDisable()
	{
		DolocAPI.Sound.UnregisterSpatialAudioListener(base.gameObject);
		base.gameObject.layer = LayerMask.NameToLayer("Default");
	}

	private void OnDrawGizmos()
	{
		if (bodyCollider != null)
		{
			GizmosHelper.DrawColliderBox(bodyCollider, Color.yellow);
		}
	}

	public void UseFishRod(ItemFishingRod fishingRod)
	{
		if (fishingRod == null || !DolocAPI.HasEnoughEnergy(DolocAPI.GlobalParameter.FishingEnergyCost))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrLackOfEnergy);
			DolocAPI.RaiseEmotionLimited(base.transform, EmotionName.EMBARRASSED);
		}
		else
		{
			FishingCache.FishingRod = fishingRod;
			StateManager.Overwrite<AgentStateFishingReady>();
		}
	}

	public void SetFishingPool(FishingPool pool)
	{
		FishingCache.FishingPool = pool;
	}

	public void SetAttackable(bool value)
	{
		_attackableType = (value ? AttackableType.Player : AttackableType.Unattackable);
	}

	public bool OnAttacked(float attack, bool criticalRate, Vector2 position, out bool isDead)
	{
		isDead = false;
		if (IsFaint)
		{
			return false;
		}
		if (RSUtils.Dice(0.1f))
		{
			base.transform.RaiseEmotion(RSUtils.Dice(0.5f) ? EmotionName.SAD : EmotionName.CRY);
		}
		int num = BattleUtils.CalcDamage(attack, CurrentDefend, criticalRate);
		if (DolocAPI.AgentEquipmentManager.TryGetShieldItem(out var item))
		{
			if (item.TryBlockAttack(num, out var blockedDamage))
			{
				DolocAPI.RaiseDamageTip(0, position);
				HatRenderer.Shine();
				HitBack(position);
				return true;
			}
			num -= blockedDamage;
		}
		DolocAPI.RaiseDamageTip(num, position);
		DolocAPI.Broadcast(GameEventType.HURT_BY_MONSTER);
		if (DolocAPI.CostHealth(num, HurtReason.MonsterAttack))
		{
			isDead = true;
		}
		else
		{
			if (StateManager.current is AgentStateFishing)
			{
				AgentStateFishing.UnsetUiControl();
				fishRodRenderer.SetVisible(value: false);
			}
			StateManager.Overwrite<AgentStateHit>();
		}
		HitBack(position);
		return true;
	}

	public bool OnSwordAttack(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		return OnAttacked(attack, criticalRate, pos, out isDead);
	}

	public void HitBack(Vector2 hitpos)
	{
		Vector2 vector = new Vector2(Mathf.Sign(((Vector2)base.transform.position).x - hitpos.x), 1f);
		rigidbody2d.velocity = Vector2.zero;
		rigidbody2d.AddForce(vector * DolocAPI.GlobalParameter.HitBackDistance, ForceMode2D.Impulse);
		base.transform.localScale = new Vector3(0f - vector.x, 1f, 1f);
	}

	public void OnNatureElementAttacked(Vector2 pos, float attack)
	{
		if (!IsFaint)
		{
			if (RSUtils.Dice(0.1f))
			{
				base.transform.RaiseEmotion(RSUtils.Dice(0.5f) ? EmotionName.SAD : EmotionName.CRY);
			}
			int value = (int)attack;
			DolocAPI.RaiseDamageTip(value, pos);
			DolocAPI.CostHealth(value, HurtReason.NatureAttack);
			DolocAPI.Broadcast(GameEventType.HURT_BY_OTHER);
		}
	}

	public void OnBomb(float damage, bool isCritical, Vector2 pos)
	{
		Vector2 vector = bodyCollider.ClosestPoint(pos);
		DolocAPI.RaiseInstantAnimEffects(vector, InstAnimEffectType.IMPACT_01);
		OnAttacked(damage, isCritical, vector, out var _);
		HitBack(vector);
	}
}
