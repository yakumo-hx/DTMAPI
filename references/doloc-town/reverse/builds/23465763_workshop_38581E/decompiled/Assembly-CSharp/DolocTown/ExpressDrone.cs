using System;
using System.Collections.Generic;
using DG.Tweening;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[GameEntityManager("/farm/express_drone", DolocGameAssets.GAME_ENTITY_EXPRESS_DRONE)]
public class ExpressDrone : GameEntity
{
	public enum DroneMovingMode
	{
		Proc_Launch_Promote,
		Proc_Launch_TakeOff,
		Proc_Recycle_Promote,
		Proc_Recycle_Landing
	}

	public enum DroneMission
	{
		None,
		ProcLanuch,
		ProcRecycle,
		ProcOpenDoor
	}

	[SerializeField]
	protected ExpressDroneConfig config;

	[SerializeField]
	protected Animator droneAnimator;

	[SerializeField]
	protected Animator vEngineAnimator;

	[SerializeField]
	protected Animator hEngineAnimator;

	[SerializeField]
	protected GameObject engineParticleDtPointL;

	[SerializeField]
	protected GameObject engineParticleDtPointR;

	[SerializeField]
	protected ParticleSystem engineParticleL;

	[SerializeField]
	protected ParticleSystem engineParticleR;

	[SerializeField]
	protected float detectDistance = 2f;

	[SerializeField]
	[Range(1f, 10f)]
	protected float maxEfficiency = 1000f;

	[SerializeField]
	protected float efficiencyAcc = 0.2f;

	[SerializeField]
	protected AnimationCurve engineEfficiencyCurve;

	[SerializeField]
	protected LayerMask groundLayer;

	[SerializeField]
	protected float takeOffHoverDuration = 3f;

	[SerializeField]
	protected float takeOffModeChangeDuration = 1.5f;

	[SerializeField]
	protected Vector2 takeOffHoverOffset = Vector2.one;

	[SerializeField]
	protected string launchSortingLayerName = "GroundFront";

	[SerializeField]
	protected float landingHoverDuration = 3f;

	[SerializeField]
	protected float eyeColorTweenDuration = 1.5f;

	[SerializeField]
	protected float landingHoverDistance = 3f;

	[SerializeField]
	protected string landingSortingLayerName;

	[SerializeField]
	protected string recycleSortingLayer;

	private Vector2 landingPosition;

	private DroneMovingMode currentMode;

	private DroneMission currentMission;

	private Vector3 currentTarget;

	private Vector2 nextPosition;

	private Vector2 currentMaxSpeed;

	private Vector2 currentAcceleration;

	private Vector2 velocity;

	private float currentMaxRotation;

	private float engineEfficiency;

	private float hoverDuration;

	private float rotation;

	private bool allowScaleX;

	private Tween eyeColorTween;

	private IEnumerable<string> AvailableSortingLayerNames
	{
		get
		{
			SortingLayer[] layers = SortingLayer.layers;
			foreach (SortingLayer sortingLayer in layers)
			{
				yield return sortingLayer.name;
			}
		}
	}

	public Action LaunchCallback { get; set; }

	public Action RecycleCallback { get; set; }

	public Action OpenDoorCallback { get; set; }

	private Vector2 ErrorVector => currentTarget - base.transform.position;

	private string DroneSortingLayerName
	{
		set
		{
			GetComponent<SpriteRenderer>().sortingLayerName = value;
			vEngineAnimator.GetComponent<SpriteRenderer>().sortingLayerName = value;
			hEngineAnimator.GetComponent<SpriteRenderer>().sortingLayerName = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		landingPosition = base.transform.position;
		Reset();
	}

	public void OpenDoor(Action callback)
	{
		currentMission = DroneMission.ProcOpenDoor;
		OpenDoorCallback = callback;
		droneAnimator.Play("load_items", 0);
	}

	public void SetLandingPosition(Vector2 position)
	{
		landingPosition = position;
		base.transform.position = position;
	}

	public void Reset()
	{
		LaunchCallback = null;
		RecycleCallback = null;
		currentMission = DroneMission.None;
		currentMode = DroneMovingMode.Proc_Launch_TakeOff;
		engineEfficiency = 0f;
		hoverDuration = 0f;
		velocity = Vector2.zero;
		rotation = 0f;
		base.transform.position = landingPosition;
		base.transform.rotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
		droneAnimator.Play("none", 0);
		vEngineAnimator.gameObject.SetActive(value: false);
		hEngineAnimator.gameObject.SetActive(value: false);
		engineParticleL.Stop();
		engineParticleR.Stop();
		SwitchEyeColor(value: false, 0f);
		DroneSortingLayerName = landingSortingLayerName;
	}

	private void Shutdown()
	{
		currentMission = DroneMission.None;
		currentMode = DroneMovingMode.Proc_Launch_TakeOff;
		engineEfficiency = 0f;
		hoverDuration = 0f;
		velocity = Vector2.zero;
		rotation = 0f;
		base.transform.position = landingPosition;
		DolocAPI.cameraController.ShakeScreen(0.2f, 0.15f);
		DolocAPI.RaiseInstantAnimEffects(landingPosition, InstAnimEffectType.LARGE_SMOKE);
		base.transform.rotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
		droneAnimator.Play("none", 0);
		vEngineAnimator.gameObject.SetActive(value: false);
		hEngineAnimator.gameObject.SetActive(value: false);
		engineParticleL.Stop();
		engineParticleR.Stop();
		SwitchEyeColor(value: false, eyeColorTweenDuration, RecycleCallback);
		DroneSortingLayerName = landingSortingLayerName;
	}

	public void Launch(Vector2 remotePosition, Action callback = null)
	{
		LaunchCallback = callback;
		currentMission = DroneMission.ProcLanuch;
		currentMode = DroneMovingMode.Proc_Launch_TakeOff;
		vEngineAnimator.gameObject.SetActive(value: true);
		vEngineAnimator.Play("vertical_engine_on", 0);
		hEngineAnimator.gameObject.SetActive(value: false);
		SwitchEyeColor(value: true, eyeColorTweenDuration);
		nextPosition = remotePosition;
		velocity = Vector2.zero;
		engineEfficiency = 0f;
		hoverDuration = 0f;
		currentTarget = landingPosition + takeOffHoverOffset;
		SetCurrentPerformance(config.performanceTakeOff);
		DroneSortingLayerName = launchSortingLayerName;
	}

	public void Recycle(Vector2 remotePosition, Action callback = null)
	{
		RecycleCallback = callback;
		currentMission = DroneMission.ProcRecycle;
		currentMode = DroneMovingMode.Proc_Recycle_Promote;
		vEngineAnimator.gameObject.SetActive(value: true);
		hEngineAnimator.gameObject.SetActive(value: true);
		vEngineAnimator.Play("vertical_engine_on", 0);
		hEngineAnimator.Play("horizontal_engine_on", 0);
		SwitchEyeColor(value: true, 0f);
		base.transform.position = remotePosition;
		currentTarget = new Vector2(landingPosition.x, landingPosition.y + landingHoverDistance);
		nextPosition = landingPosition;
		velocity = Vector2.zero;
		engineEfficiency = 0f;
		hoverDuration = 0f;
		allowScaleX = true;
		SetCurrentPerformance(config.performancePromote);
		DroneSortingLayerName = recycleSortingLayer;
	}

	private void Update()
	{
		switch (currentMission)
		{
		case DroneMission.ProcLanuch:
			UpdateLaunch(Time.deltaTime);
			break;
		case DroneMission.ProcRecycle:
			UpdateRecycle(Time.deltaTime);
			break;
		case DroneMission.ProcOpenDoor:
		{
			AnimatorStateInfo currentAnimatorStateInfo = droneAnimator.GetCurrentAnimatorStateInfo(0);
			if (currentAnimatorStateInfo.IsName("load_items") && currentAnimatorStateInfo.normalizedTime >= 1f)
			{
				OpenDoorCallback?.Invoke();
				OpenDoorCallback = null;
			}
			break;
		}
		case DroneMission.None:
			break;
		}
	}

	private void UpdateLaunch(float dt)
	{
		float magnitude = currentMaxSpeed.magnitude;
		switch (currentMode)
		{
		case DroneMovingMode.Proc_Launch_TakeOff:
		{
			if (engineEfficiency < maxEfficiency)
			{
				engineEfficiency += efficiencyAcc * Time.deltaTime;
			}
			else
			{
				hoverDuration += dt;
				if (hoverDuration >= takeOffHoverDuration)
				{
					hoverDuration = 0f;
					currentMode = DroneMovingMode.Proc_Launch_Promote;
				}
			}
			float num = engineEfficiencyCurve.Evaluate(engineEfficiency / maxEfficiency);
			velocity += CalcSmackAcc(ErrorVector, currentAcceleration * num) * dt;
			break;
		}
		case DroneMovingMode.Proc_Launch_Promote:
			hoverDuration += dt;
			if (hoverDuration >= takeOffModeChangeDuration)
			{
				allowScaleX = true;
				currentTarget = nextPosition;
				SetCurrentPerformance(config.performancePromote);
			}
			velocity += CalcParkingAcc(velocity, ErrorVector, magnitude, 6f) * dt;
			if ((nextPosition - (Vector2)base.transform.position).magnitude < 0.5f)
			{
				LaunchCallback?.Invoke();
				LaunchCallback = null;
			}
			break;
		}
		velocity = Vector2.ClampMagnitude(velocity, magnitude);
		base.transform.Translate(velocity * dt, Space.World);
		UpdateRenderState();
	}

	private void UpdateRecycle(float dt)
	{
		float magnitude = currentMaxSpeed.magnitude;
		switch (currentMode)
		{
		case DroneMovingMode.Proc_Recycle_Promote:
			if (ErrorVector.magnitude < 0.3f)
			{
				allowScaleX = false;
				hoverDuration += dt;
				if (hoverDuration >= landingHoverDuration)
				{
					currentTarget = nextPosition;
					base.transform.rotation = Quaternion.identity;
					base.transform.localScale = Vector3.one;
					currentMode = DroneMovingMode.Proc_Recycle_Landing;
					SetCurrentPerformance(config.performanceLanding);
				}
			}
			velocity += CalcParkingAcc(velocity, ErrorVector, magnitude, 6f) * dt;
			UpdateRenderState();
			break;
		case DroneMovingMode.Proc_Recycle_Landing:
			velocity += CalcParkingAcc(velocity, ErrorVector, magnitude, 6f) * dt;
			if (ErrorVector.magnitude < 0.2f)
			{
				Shutdown();
			}
			UpdateRenderState();
			break;
		}
		velocity = Vector2.ClampMagnitude(velocity, magnitude);
		base.transform.Translate(velocity * dt, Space.World);
	}

	private void UpdateRenderState(bool rotate = true, bool engineParticle = true, bool engineAnimator = true, bool allowScaleX = false)
	{
		if (rotate && currentMaxSpeed.x != 0f)
		{
			rotation = velocity.x / currentMaxSpeed.x * currentMaxRotation;
			base.transform.rotation = Quaternion.Euler(0f, 0f, 0f - rotation);
		}
		if (engineParticle)
		{
			EngineParticleProbe(engineParticleDtPointL.transform.position, engineParticleL);
			EngineParticleProbe(engineParticleDtPointR.transform.position, engineParticleR);
		}
		if (engineAnimator)
		{
			if (Mathf.Abs(velocity.x) > 0.3f)
			{
				if (!hEngineAnimator.gameObject.activeSelf)
				{
					hEngineAnimator.gameObject.SetActive(value: true);
					hEngineAnimator.Play("horizontal_engine_on", 0);
				}
			}
			else if (hEngineAnimator.gameObject.activeSelf)
			{
				hEngineAnimator.gameObject.SetActive(value: false);
			}
		}
		if (allowScaleX)
		{
			if (velocity.x > 0f)
			{
				base.transform.localScale = new Vector3(-1f, 1f, 1f);
			}
			else if (velocity.x < 0f)
			{
				base.transform.localScale = new Vector3(1f, 1f, 1f);
			}
		}
	}

	private void EngineParticleProbe(Vector2 point, ParticleSystem engineParticle)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(point, Vector2.down, detectDistance, groundLayer);
		if (raycastHit2D.collider != null)
		{
			engineParticle.transform.position = raycastHit2D.point;
			if (!engineParticle.isPlaying)
			{
				engineParticle.Play();
			}
		}
		else if (engineParticle.isPlaying)
		{
			engineParticle.Stop();
		}
	}

	private void SwitchEyeColor(bool value, float duration, Action callback = null)
	{
		if (duration == 0f)
		{
			GetComponent<SpriteRenderer>().sharedMaterial.SetFloat("_EyeColorIntensity", value ? 1 : 0);
			return;
		}
		eyeColorTween?.Kill();
		Material mat = GetComponent<SpriteRenderer>().sharedMaterial;
		float num = ((!value) ? 1 : 0);
		float endValue = 1f - num;
		ColorSetter(num);
		eyeColorTween = DOTween.To(ColorGetter, ColorSetter, endValue, duration).OnComplete(delegate
		{
			callback?.Invoke();
		});
		float ColorGetter()
		{
			return mat.GetFloat("_EyeColorIntensity");
		}
		void ColorSetter(float value)
		{
			mat.SetFloat("_EyeColorIntensity", value);
		}
	}

	private void SetCurrentPerformance(ExpressDronePerformance performance)
	{
		currentMaxSpeed = performance.maxSpeed;
		currentAcceleration = performance.acceleration;
		currentMaxRotation = performance.maxRotation;
	}

	private static Vector2 CalcParkingAcc(Vector2 velocity, Vector2 errorVec, float maxSpeed, float parkingRadius, float responseDuration = 0.75f)
	{
		float magnitude = errorVec.magnitude;
		float num = ((magnitude > parkingRadius) ? maxSpeed : (maxSpeed * (magnitude / parkingRadius)));
		return (errorVec.normalized * num - velocity) / responseDuration;
	}

	private static Vector2 CalcSmackAcc(Vector2 errorVec, Vector2 acceleration)
	{
		return errorVec * acceleration;
	}
}
