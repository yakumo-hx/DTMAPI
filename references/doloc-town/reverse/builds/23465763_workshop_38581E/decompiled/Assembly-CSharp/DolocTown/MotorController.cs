using System;
using System.Collections;
using System.Collections.Generic;
using DolocTown.UI;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MotorController : DolocObject
{
	[SerializeField]
	private float maxRotation;

	[SerializeField]
	private Vector2 enduranceProgressOffset;

	[SerializeField]
	private Transform droneFollowPoint;

	[SerializeField]
	private GameObject hiddenTrigger;

	[SerializeField]
	private GameObject droneInteractableBox;

	[SerializeField]
	private bool isDebug;

	[SerializeField]
	private AnimationCurve natureJumpCurve;

	[SerializeField]
	private float accelerationX;

	[SerializeField]
	private float accelerationRX;

	[SerializeField]
	private float decelerationX;

	[SerializeField]
	private float maxSpeedX;

	[SerializeField]
	[Min(0f)]
	private float horizontalReboundSpeedRate;

	[SerializeField]
	private float gravity;

	[SerializeField]
	private float maxDropSpeed;

	[SerializeField]
	private float groundRayLength;

	[SerializeField]
	private Vector2 naturalJumpAccelerationRange;

	[SerializeField]
	private float naturalJumpThreshold;

	[SerializeField]
	private float maxNatureJumpSpeed;

	[SerializeField]
	[Min(0f)]
	private float maxVerticalReboundSpeed;

	[SerializeField]
	[Min(0f)]
	private float verticalReboundSpeedRate;

	[SerializeField]
	private float pushJumpAcceleration;

	[SerializeField]
	private float maxPushJumpSpeed;

	[SerializeField]
	private float enduranceDuration;

	[SerializeField]
	private float enduranceRecoveryRate;

	[SerializeField]
	private bool pauseY;

	private MotorLight motorLight;

	private float currentVelocityX;

	private float currentVelocityY;

	private float currentInputX;

	private float currentInputY;

	private float currentDstToGround;

	private float currentEndurance;

	private bool isFlying;

	private bool isFollowing;

	private Coroutine autoFollowCoroutine;

	private ProgressCircle enduranceProgressCircle;

	private Vector2 velocityCache;

	private bool motorForeground;

	private bool _reboundLatch;

	private float AccelerationX => DolocAPI.GlobalParameter.MotorHorizontalAcceleration;

	private float AccelerationRX => DolocAPI.GlobalParameter.MotorHorizontalRevertAcceleration;

	private float DecelerationX => DolocAPI.GlobalParameter.MotorHorizontalDeceleration;

	private float MaxSpeedX => DolocAPI.GlobalParameter.MotorHorizontalMaxSpeed;

	private float Gravity => DolocAPI.GlobalParameter.MotorGravity;

	private float MaxDropSpeed => DolocAPI.GlobalParameter.MotorDropMaxSpeed;

	private float GroundRayLength => DolocAPI.GlobalParameter.MotorRaycastLength;

	private Vector2 NaturalJumpAccelerationRange => DolocAPI.GlobalParameter.MotorNaturalJumpAccRange;

	private float NaturalJumpThreshold => DolocAPI.GlobalParameter.MotorNaturalJumpThreshold;

	private float MaxNaturalJumpSpeed => DolocAPI.GlobalParameter.MotorNaturalJumpMaxSpeed;

	private float PushJumpAcceleration => DolocAPI.GlobalParameter.MotorVerticalAcceleration;

	private float MaxPushJumpSpeed => DolocAPI.GlobalParameter.MotorVerticalMaxSpeed;

	private float EnduranceDuration => DolocAPI.GlobalParameter.MotorEnduranceDuration;

	private float EnduranceRecoveryRate => DolocAPI.GlobalParameter.MotorEnduranceRecv;

	private float HorizontalReboundSpeedRate => DolocAPI.GlobalParameter.MotorHorizontalReboundSpeedRate;

	private float MaxVerticalReboundSpeed => DolocAPI.GlobalParameter.MotorCollisionVerticalSpeed;

	private float VerticalReboundSpeedRate => DolocAPI.GlobalParameter.MotorVerticalReboundSpeedRate;

	public Transform DroneFollowPoint => droneFollowPoint;

	public Rigidbody2D rb { get; private set; }

	public MotorDriverRenderer driverRenderer { get; private set; }

	public MotorRenderer motorRenderer { get; private set; }

	public ScannerGate scannerGate { get; private set; }

	public ScannerInteractableOfMotor scannerInteractable { get; private set; }

	public MotorInteractable motorInteractable { get; private set; }

	public bool IsRiding { get; private set; }

	public bool IsFaceRight
	{
		get
		{
			return base.transform.localScale.x > 0f;
		}
		set
		{
			base.transform.localScale = new Vector3(value ? 1 : (-1), 1f, 1f);
		}
	}

	public float EnduranceProgress => currentEndurance / EnduranceDuration;

	protected override void __Init()
	{
		base.__Init();
		rb = GetComponent<Rigidbody2D>();
		driverRenderer = GetComponentInChildren<MotorDriverRenderer>(includeInactive: true);
		driverRenderer.Init();
		motorLight = GetComponentInChildren<MotorLight>(includeInactive: true);
		motorLight.Init();
		scannerGate = GetComponentInChildren<ScannerGate>(includeInactive: true);
		scannerGate.Init();
		scannerInteractable = GetComponentInChildren<ScannerInteractableOfMotor>(includeInactive: true);
		scannerInteractable.Init();
		enduranceProgressCircle = DolocGameAssets.UI_ELEMENT_PROGRESSBAR_CIRCLE.CreateEntity<ProgressCircle>(DolocAPI.uiSystem.rectTransform);
		motorInteractable = GetComponentInChildren<MotorInteractable>(includeInactive: true);
		motorInteractable.Init();
		enduranceProgressCircle.SetVisible(value: false);
		motorRenderer = GetComponentInChildren<MotorRenderer>(includeInactive: true);
		SetMotorForeground(value: true);
		SetVisible(value: false);
		SetIsRiding(value: false);
	}

	public void Control(float inputX, float inputY)
	{
		if (base.isVisible)
		{
			currentInputX = inputX;
			currentInputY = inputY;
			if (currentInputX != 0f)
			{
				IsFaceRight = currentInputX > 0f;
			}
			motorRenderer.ShouldRun = inputY > 0f && currentEndurance > 0f;
			UpdateRotation();
			UpdateUiPosition();
		}
	}

	private IEnumerable<SpriteRenderer> GetAllSpriteRenderers()
	{
		yield return GetComponent<SpriteRenderer>();
		SpriteRenderer[] componentsInChildren = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			yield return componentsInChildren[i];
		}
	}

	public void SetMotorForeground(bool value)
	{
		motorForeground = value;
		if (motorForeground)
		{
			foreach (SpriteRenderer allSpriteRenderer in GetAllSpriteRenderers())
			{
				allSpriteRenderer.sortingLayerName = "GroundFront";
				allSpriteRenderer.sortingOrder = 0;
			}
			return;
		}
		foreach (SpriteRenderer allSpriteRenderer2 in GetAllSpriteRenderers())
		{
			allSpriteRenderer2.sortingLayerName = "Npc";
			allSpriteRenderer2.sortingOrder = 0;
		}
	}

	public void AutoFlyTo(Func<Vector2> positionGetter, Action callback = null)
	{
		isFollowing = true;
		if (TryGetComponent<Rigidbody2D>(out var component))
		{
			component.bodyType = RigidbodyType2D.Kinematic;
		}
		if (positionGetter == null)
		{
			positionGetter = () => base.transform.position;
		}
		if (autoFollowCoroutine != null)
		{
			return;
		}
		motorInteractable.SetVisible(value: false);
		scannerGate.SetVisible(value: false);
		scannerInteractable.SetVisible(value: false);
		autoFollowCoroutine = StartCoroutine(FlyToTargetCoroutine(positionGetter, delegate
		{
			isFollowing = false;
			autoFollowCoroutine = null;
			motorInteractable.SetVisible(value: true);
			if (TryGetComponent<Rigidbody2D>(out var component2))
			{
				component2.bodyType = RigidbodyType2D.Dynamic;
			}
			callback?.Invoke();
		}));
	}

	private IEnumerator FlyToTargetCoroutine(Func<Vector2> positionGetter, Action callback = null)
	{
		Vector2 targetPos = positionGetter();
		while (((Vector2)base.transform.position - targetPos).magnitude > 1f)
		{
			targetPos = positionGetter();
			IsFaceRight = targetPos.x > base.transform.position.x;
			Vector3 vector = (targetPos - (Vector2)base.transform.position).normalized;
			base.transform.position += vector * (DolocAPI.GlobalParameter.MotorCallSpeed * Time.deltaTime);
			yield return null;
		}
		callback?.Invoke();
	}

	public void StopFollowing()
	{
		isFollowing = false;
		autoFollowCoroutine = null;
		motorInteractable.SetVisible(value: true);
		if (TryGetComponent<Rigidbody2D>(out var component))
		{
			component.bodyType = RigidbodyType2D.Dynamic;
		}
	}

	public void SetIsRiding(bool value)
	{
		if (IsRiding != value)
		{
			StopFollowing();
			DolocAPI.Sound.PostSoundEvent(value ? SoundEvents.PLAY_GET_ON_MOTOR : SoundEvents.PLAY_GET_OFF_MOTOR);
		}
		IsRiding = value;
		_reboundLatch = false;
		base.gameObject.layer = LayerMask.NameToLayer(IsRiding ? "PlatformExclude" : "GroundExcludeAgent");
		scannerGate.SetVisible(IsRiding);
		scannerInteractable.SetVisible(IsRiding);
		enduranceProgressCircle.SetVisible(value && currentEndurance < EnduranceDuration);
		UpdateUiPosition();
		driverRenderer.SetVisible(value);
		string text = (value ? "Player" : "Untagged");
		driverRenderer.tag = text;
		droneInteractableBox.tag = text;
		hiddenTrigger.gameObject.SetActive(value: false);
		hiddenTrigger.gameObject.SetActive(value);
		if (!value)
		{
			ClearVelocity();
			base.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
		}
		motorLight.CheckRiding();
	}

	private void RecordVelocity()
	{
		velocityCache = rb.velocity;
		rb.velocity = Vector2.zero;
	}

	private void RestoreVelocity()
	{
		rb.velocity = velocityCache;
	}

	public void PauseRigidbody()
	{
		RecordVelocity();
		rb.isKinematic = true;
		if (enduranceProgressCircle.isVisible)
		{
			enduranceProgressCircle.SetVisible(value: false);
		}
	}

	public void ResumeRigidbody()
	{
		rb.isKinematic = false;
		RestoreVelocity();
		if (IsRiding && currentEndurance < EnduranceDuration)
		{
			enduranceProgressCircle.SetVisible(value: true);
		}
	}

	private void ClearVelocity()
	{
		rb.velocity = Vector2.zero;
		currentVelocityX = 0f;
		currentVelocityY = 0f;
		currentInputX = 0f;
		currentInputY = 0f;
	}

	public bool CheckVelocityAbs(float threshold)
	{
		return Mathf.Abs(currentVelocityX) > threshold;
	}

	public bool CheckInputAbs(float threshold)
	{
		return Mathf.Abs(currentInputX) > threshold;
	}

	private void UpdateVelocityX(float dt)
	{
		if (currentInputX != 0f)
		{
			float num = ((Mathf.Sign(currentInputX) != Mathf.Sign(currentVelocityX)) ? AccelerationRX : AccelerationX);
			currentVelocityX += dt * num * Mathf.Sign(currentInputX);
			currentVelocityX = Mathf.Clamp(currentVelocityX, 0f - MaxSpeedX, MaxSpeedX);
		}
		else if (Mathf.Abs(currentVelocityX) > 0f)
		{
			currentVelocityX -= dt * DecelerationX * Mathf.Sign(currentVelocityX);
			if (Mathf.Abs(currentVelocityX) < 0.1f)
			{
				currentVelocityX = 0f;
			}
		}
	}

	private void UpdateUiPosition()
	{
		if (enduranceProgressCircle.isVisible)
		{
			Vector2 vector = (Vector2)base.transform.position + enduranceProgressOffset;
			enduranceProgressCircle.position = DolocAPI.WorldToScreen(vector);
		}
	}

	private void UpdateRotation()
	{
		if (currentVelocityX != 0f)
		{
			float num = Mathf.Abs(currentVelocityX) / MaxSpeedX * maxRotation;
			base.transform.rotation = Quaternion.Euler(0f, 0f, num * base.transform.localScale.x);
		}
		else
		{
			base.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
		}
	}

	private void UpdateVelocityY(float dt)
	{
		if (pauseY)
		{
			return;
		}
		if (currentInputY != 0f)
		{
			if (currentInputY > 0f)
			{
				if (currentEndurance > 0f)
				{
					CostEndurance(dt);
					currentVelocityY += dt * currentInputY * PushJumpAcceleration;
					currentVelocityY = Mathf.Min(currentVelocityY, MaxPushJumpSpeed);
				}
				else
				{
					UpdateVelocityYWhileNoInput(dt);
				}
			}
			else
			{
				currentVelocityY += dt * currentInputY * PushJumpAcceleration;
				currentVelocityY = Mathf.Min(currentVelocityY, MaxPushJumpSpeed);
			}
		}
		else
		{
			UpdateVelocityYWhileNoInput(dt);
		}
	}

	private void UpdateVelocityYWhileNoInput(float dt)
	{
		RecoveryEndurance(dt);
		currentDstToGround = GetDstToGround(GroundRayLength);
		if (isFlying && currentDstToGround < GroundRayLength)
		{
			isFlying = false;
		}
		if (currentDstToGround > NaturalJumpThreshold)
		{
			currentVelocityY -= Gravity * dt;
			currentVelocityY = Mathf.Max(currentVelocityY, 0f - MaxDropSpeed);
			return;
		}
		float t = natureJumpCurve.Evaluate(1f - currentDstToGround / NaturalJumpThreshold);
		Vector2 vector = NaturalJumpAccelerationRange;
		currentVelocityY += Time.fixedDeltaTime * Mathf.Lerp(vector.x, vector.y, t);
		currentVelocityY = Mathf.Min(currentVelocityY, MaxNaturalJumpSpeed);
	}

	private void CostEndurance(float dt)
	{
		if (!(currentEndurance <= 0f))
		{
			isFlying = true;
			enduranceProgressCircle.SetVisible(IsRiding);
			currentEndurance -= dt;
			if (currentEndurance <= 0f)
			{
				currentEndurance = 0f;
				enduranceProgressCircle.Progress = 0f;
			}
			enduranceProgressCircle.Progress = EnduranceProgress;
		}
	}

	private void RecoveryEndurance(float dt)
	{
		if (!isFlying)
		{
			currentEndurance += dt * EnduranceRecoveryRate;
			if (currentEndurance >= EnduranceDuration)
			{
				currentEndurance = EnduranceDuration;
				enduranceProgressCircle.SetVisible(value: false);
			}
			else
			{
				enduranceProgressCircle.Progress = EnduranceProgress;
			}
		}
	}

	private float GetDstToGround(float rayLength)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, Vector2.down, rayLength, (int)DolocAPI.gameConfig.walkableMask | (int)DolocAPI.gameConfig.waterMask);
		if (raycastHit2D.collider != null)
		{
			return Mathf.Max(raycastHit2D.distance, 0f);
		}
		Room currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom != null)
		{
			if (currentRoom.IsInHouse)
			{
				return base.transform.position.y - currentRoom.RoomPosition.y;
			}
			return base.transform.position.y - currentRoom.ScenePosition.y;
		}
		return rayLength;
	}

	public override void SetVisible(bool value)
	{
		base.SetVisible(value);
		if (!value)
		{
			StopFollowing();
		}
		enduranceProgressCircle.SetVisible(IsRiding && value);
		UpdateUiPosition();
		if (value)
		{
			motorLight.CheckStatus();
		}
	}

	public void Reset()
	{
		IsRiding = false;
		isFlying = false;
		currentInputX = 0f;
		currentInputY = 0f;
		currentVelocityX = 0f;
		currentVelocityY = 0f;
		currentEndurance = EnduranceDuration;
		velocityCache = Vector2.zero;
		enduranceProgressCircle.SetVisible(value: false);
		ClearVelocity();
		motorLight.CheckRiding();
	}

	public void UpdatePerTU()
	{
		driverRenderer.UpdatePerTU();
	}

	public void OnFixedUpdate(float dt)
	{
		motorLight.OnFixedUpdate(dt);
		if (!isFollowing)
		{
			if (IsRiding)
			{
				_ = rb.velocity;
				UpdateVelocityX(dt);
				UpdateVelocityY(dt);
				rb.velocity = new Vector2(currentVelocityX, currentVelocityY);
			}
			else
			{
				currentVelocityX = 0f;
				UpdateVelocityY(dt);
				rb.velocity = new Vector2(currentVelocityX, currentVelocityY);
			}
		}
	}

	private IEnumerator ResumeReboundLatch(float wait = 0.1f)
	{
		yield return new WaitForSeconds(wait);
		_reboundLatch = false;
	}

	private void OnCollisionEnter2D(Collision2D other)
	{
		if (RSUtils.LayerMaskCheck(other.gameObject, DolocAPI.gameConfig.motorReboundMask) && !_reboundLatch)
		{
			_reboundLatch = true;
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_MOTOR_IMPACT);
			StartCoroutine(ResumeReboundLatch());
			Vector2 normal = other.GetContact(0).normal;
			Vector2 vector = new Vector2(currentVelocityX, currentVelocityY);
			float magnitude = vector.magnitude;
			Vector2 normalized = Vector2.Reflect(vector.normalized, normal).normalized;
			currentVelocityX = normalized.x * magnitude * HorizontalReboundSpeedRate;
			currentVelocityY = Mathf.Min(normalized.y * magnitude * VerticalReboundSpeedRate, maxVerticalReboundSpeed);
			rb.velocity = new Vector2(currentVelocityX, currentVelocityY);
		}
	}
}
