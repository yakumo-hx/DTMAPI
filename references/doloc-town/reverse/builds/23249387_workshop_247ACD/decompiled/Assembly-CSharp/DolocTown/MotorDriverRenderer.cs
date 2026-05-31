using DolocTown.Config.Player;
using Febucci.Attributes;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class MotorDriverRenderer : DolocObject
{
	[SerializeField]
	[MinValue(0f)]
	private float velocityThresholdOfAnimation = 12f;

	[SerializeField]
	[MinValue(0f)]
	private float inputThresholdOfAccelerate = 0.1f;

	private MotorController _motorController;

	private Animator _animator;

	private AgentHatRenderer defaultHatRenderer;

	private AgentHatRenderer HatRenderer;

	private PlayerSplitRenderer splitRenderer;

	private string _currentAnimation;

	private SoundEvents _currentSound;

	private string AnimationName
	{
		get
		{
			if (!_motorController.CheckVelocityAbs(velocityThresholdOfAnimation))
			{
				return "ride_idle";
			}
			return "ride_run";
		}
	}

	private SoundEvents SoundName
	{
		get
		{
			if (!_motorController.CheckInputAbs(inputThresholdOfAccelerate))
			{
				return SoundEvents.PLAY_MOTOR_COOL_DOWN;
			}
			return SoundEvents.PLAY_MOTOR_ACCELERATE;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		_animator = GetComponent<Animator>();
		splitRenderer = GetComponentInChildren<PlayerSplitRenderer>(includeInactive: true);
		splitRenderer.Init();
		defaultHatRenderer = GetComponentInChildren<AgentHatRenderer>(includeInactive: true);
		defaultHatRenderer.InitHat();
		HatRenderer = defaultHatRenderer;
		_motorController = GetComponentInParent<MotorController>(includeInactive: true);
		_currentAnimation = "ride_idle";
		_currentSound = SoundEvents.PLAY_MOTOR_IDLE;
		SetVisible(value: false);
	}

	public void UpdatePerTU()
	{
		HatRenderer.UpdatePerTU();
	}

	public void SetHatInfo(HatInfo info = null)
	{
		if (info == null)
		{
			HatRenderer.CurrentHatRenderInfo = null;
			splitRenderer.Play("ride_idle");
			return;
		}
		if (AgentHatRendererUtils.TryLoadHatRenderer(info.Id, isRiding: true, out var hatRenderer))
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
		AnimatorStateInfo currentAnimatorStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
		AnimatorClipInfo[] currentAnimatorClipInfo = _animator.GetCurrentAnimatorClipInfo(0);
		if (currentAnimatorClipInfo.Length != 0)
		{
			string animName = currentAnimatorClipInfo[0].clip.name;
			HatRenderer.Play(animName, currentAnimatorStateInfo.normalizedTime);
			splitRenderer.Play(animName);
		}
	}

	private void PlayAnimation(string name)
	{
		_animator.Play(name);
		HatRenderer.Play(name);
		splitRenderer.Play(name);
	}

	private void OnEnable()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_MOTOR_IDLE);
		_currentSound = SoundEvents.PLAY_MOTOR_COOL_DOWN;
		_currentAnimation = "";
	}

	private void OnDisable()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_MOTOR_ALL);
	}

	private void Update()
	{
		string animationName = AnimationName;
		if (animationName != _currentAnimation)
		{
			PlayAnimation(animationName);
			_currentAnimation = animationName;
		}
		SoundEvents soundName = SoundName;
		if (soundName != _currentSound)
		{
			DolocAPI.Sound.PostSoundEvent(soundName);
			_currentSound = soundName;
		}
	}
}
