using UnityEngine;

namespace DolocTown;

public class InteractiveWater : InteractableObjectExclude, IMonsterInteractable, IWindInteractive, IBombInteractive
{
	private bool inSplashAnim;

	private Vector2 manuallyOffset;

	private float latestSoundTime;

	protected override ITouchCheckStrategy touchChecker { get; set; }

	private WaterTouchChecker checker => touchChecker as WaterTouchChecker;

	public WaterController waterController { get; private set; }

	public static bool IsInWater { get; private set; }

	public static InteractiveWater CurrentWater { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		touchChecker = new WaterTouchChecker();
		waterController = GetComponent<WaterController>();
		manuallyOffset = new Vector2(0f, -0.25f);
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		IsInWater = true;
		CurrentWater = this;
		OnEnterWater();
	}

	protected override void OnDisTouch()
	{
		IsInWater = false;
		CurrentWater = this;
		OnExitWater();
		base.OnDisTouch();
	}

	private Vector2 GetBestSplashPosition(Vector2 pos)
	{
		return _collider.ClosestPoint(pos) + manuallyOffset;
	}

	private void OnEnterWater()
	{
		waterController.Wave(DolocAPI.AgentPosition, waterController.waveParamOnEnter, reverse: true);
		DolocAPI.AbilitySystem.motionAbility.ComposeEnvModerate(waterController.waterModerate);
		if (!inSplashAnim && checker.playerVelocity.y < 0f)
		{
			inSplashAnim = true;
			Vector2 bestSplashPosition = GetBestSplashPosition(DolocAPI.AgentPosition);
			DolocAPI.RaiseInstantAnimEffects(bestSplashPosition, InstAnimEffectType.AGENT_SPLASH);
			DolocAPI.RaiseInstantPSEffects(new Vector2(bestSplashPosition.x, base.transform.position.y), InstantParticleEffectsType.WATER_BUBBLES_SHALLOW);
			DolocAPI.Delay(0.2f, delegate
			{
				inSplashAnim = false;
			});
		}
		if (Time.time - latestSoundTime > 0.3f)
		{
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_ENTER_WATER);
			latestSoundTime = Time.time;
		}
	}

	private void OnExitWater()
	{
		Vector2 bestSplashPosition = GetBestSplashPosition(DolocAPI.AgentPosition);
		waterController.Wave(bestSplashPosition, waterController.waveParamOnEnter);
		DolocAPI.AbilitySystem.motionAbility.ComposeEnvModerate(-1 * waterController.waterModerate);
	}

	public void OnWalkInWater()
	{
		waterController.Wave(DolocAPI.AgentPosition, waterController.waveParamOnStay, reverse: true);
	}

	public void OnRainDrop(Vector3 pos)
	{
		waterController.Wave(pos, waterController.waveParamOnRainDrop, reverse: true);
	}

	public void Splash(Vector2 position)
	{
		position = GetBestSplashPosition(position);
		DolocAPI.RaiseInstantAnimEffects(position, InstAnimEffectType.AGENT_SPLASH);
		waterController.Wave(position, waterController.waveParamOnEnter);
	}

	public override bool OnAttacked(float damage, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		DolocAPI.RaiseInstantAnimEffects(GetBestSplashPosition(pos), InstAnimEffectType.AGENT_SPLASH);
		waterController.Wave(pos, waterController.waveOnAttack, reverse: true);
		return true;
	}

	public void OnMonsterTouch(Vector2 pos)
	{
		DolocAPI.RaiseInstantAnimEffects(GetBestSplashPosition(pos), InstAnimEffectType.AGENT_SPLASH);
		waterController.Wave(pos, waterController.waveParamOnEnter, reverse: true);
	}

	public void OnMonsterDisTouch(Vector2 pos)
	{
		DolocAPI.RaiseInstantAnimEffects(GetBestSplashPosition(pos), InstAnimEffectType.AGENT_SPLASH);
		waterController.Wave(pos, waterController.waveParamOnEnter);
	}

	public override bool OnFell(ItemTool tool, Vector2 pos)
	{
		DolocAPI.RaiseInstantAnimEffects(_collider.ClosestPoint(pos), InstAnimEffectType.AGENT_SPLASH);
		return false;
	}

	public void OnWindBlow(Vector2 pos)
	{
		pos = GetBestSplashPosition(pos);
		waterController.Wave(pos, waterController.waveParamOnRainDrop, reverse: true);
	}

	public void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
		DolocAPI.RaiseInstantAnimEffects(GetBestSplashPosition(pos), InstAnimEffectType.AGENT_SPLASH);
		waterController.Wave(pos, waterController.waveOnBomb);
	}
}
