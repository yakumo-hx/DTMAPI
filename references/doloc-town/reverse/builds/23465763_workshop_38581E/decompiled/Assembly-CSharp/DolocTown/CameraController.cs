using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class CameraController : DolocObject
{
	[SerializeField]
	private Transform follow;

	[SerializeField]
	private float moveSpeed = 3f;

	[SerializeField]
	private float threshold = 0.1f;

	[SerializeField]
	private Vector2 movingBounds = new Vector2(100f, 100f);

	[SerializeField]
	private int shakeVibrato = 10;

	[SerializeField]
	private float randomness = 90f;

	[SerializeField]
	private bool shakeFadeOut = true;

	[SerializeField]
	private Vector2 camSize = new Vector2(60f, 33.75f);

	private Vector2 xRange;

	private Vector2 yRange;

	private Vector2 scenePosition;

	private Vector2 sceneSize;

	private Quaternion sourceRotation;

	private Action<float> moveFunc;

	private float currentMoveSpeed;

	private Tween currentMoveAction;

	private bool smoothMoving = true;

	private Tween currentTween;

	private bool _isShaking;

	private float _shakeDuration;

	private float _shakeIntensity;

	private Vector3 _totalShakeNoise;

	private bool _isNotFixedRange;

	public Vector2 CamSize => camSize;

	private Vector3 _ShakeNoise
	{
		get
		{
			if (!_isShaking)
			{
				return Vector3.zero;
			}
			return new Vector3(UnityEngine.Random.Range(0f - _shakeIntensity, _shakeIntensity), UnityEngine.Random.Range(0f - _shakeIntensity, _shakeIntensity), 0f);
		}
	}

	public override Vector2 position2d
	{
		get
		{
			return base.transform.position;
		}
		set
		{
			base.transform.position = new Vector3(value.x, value.y, base.transform.position.z);
		}
	}

	private float HorizontalError
	{
		get
		{
			float x = follow.position.x;
			float x2 = base.transform.position.x;
			float num = movingBounds.x * 0.5f;
			float num2 = x2 - num;
			float num3 = x2 + num;
			if (x < num2)
			{
				return x - num2;
			}
			if (x > num3)
			{
				return x - num3;
			}
			return 0f;
		}
	}

	private float VerticalError
	{
		get
		{
			float y = follow.position.y;
			float y2 = base.transform.position.y;
			float num = movingBounds.y * 0.5f;
			float num2 = y2 - num;
			float num3 = y2 + num;
			if (y < num2)
			{
				return y - num2;
			}
			if (y > num3)
			{
				return y - num3;
			}
			return 0f;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		base.enabled = false;
		_isNotFixedRange = false;
		moveFunc = null;
		sourceRotation = Quaternion.Euler(0f, 0f, 0f);
		currentMoveSpeed = moveSpeed;
	}

	public void ShakeScreen(float shakeDuration = 0.2f, float shakeStrength = 0.3f)
	{
		if (DolocAPI.userSettings.useScreenShake)
		{
			_ShakeScreenByRotation(shakeDuration, shakeStrength);
		}
	}

	private void _ShakeScreenByRotation(float shakeDuration = 0.2f, float shakeStrength = 0.3f)
	{
		if (currentTween != null)
		{
			currentTween.Kill();
			base.transform.rotation = sourceRotation;
		}
		Vector3 strength = new Vector3(shakeStrength, shakeStrength, 0f);
		Camera component = GetComponent<Camera>();
		currentTween = component.DOShakeRotation(shakeDuration, strength, shakeVibrato, randomness, shakeFadeOut).OnComplete(delegate
		{
			currentTween = null;
			base.transform.rotation = sourceRotation;
		});
	}

	public void SetFollow(Transform target)
	{
		if (!(target == null))
		{
			follow = target;
		}
	}

	public void SetFollow(MonoBehaviour script)
	{
		if (!(script == null))
		{
			follow = script.transform;
		}
	}

	public void SetMoveSpeed(float value = -1f)
	{
		currentMoveSpeed = ((value < 0f) ? moveSpeed : value);
	}

	public void SetMoveFunction(bool x, bool y)
	{
		if (x)
		{
			moveFunc = (y ? new Action<float>(moveXY) : new Action<float>(moveX));
		}
		else if (y)
		{
			moveFunc = moveY;
		}
	}

	public void UpdateCamPosition(float dt)
	{
		if (base.enabled)
		{
			if (smoothMoving)
			{
				moveFunc(dt);
				return;
			}
			Vector3 vector = follow.position;
			base.transform.position = new Vector3(Mathf.Clamp(vector.x, xRange.x, xRange.y), Mathf.Clamp(vector.y, yRange.x, yRange.y), base.transform.position.z);
		}
	}

	public void SetSmoothMoving(bool value)
	{
		smoothMoving = value;
	}

	public void setEnabled(bool value)
	{
		if (_isNotFixedRange)
		{
			base.enabled = value;
		}
	}

	public void MoveTo(Vector2 position, float moveSpeed, Ease moveEase = Ease.Linear, Action callback = null)
	{
		setEnabled(value: false);
		if (moveSpeed <= 0f)
		{
			moveSpeed = 1f;
		}
		Vector3 vector = Constraint(scenePosition, sceneSize, position);
		float duration = Vector2.Distance(base.transform.position, vector) / moveSpeed;
		base.transform.DOMove(vector, duration).SetEase(moveEase).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}

	public void MoveToByDuration(Vector2 position, float duration, Ease ease = Ease.Linear, Action callback = null)
	{
		setEnabled(value: false);
		currentMoveAction?.Kill();
		currentMoveAction = base.transform.DOMove(new Vector3(position.x, position.y, base.transform.position.z), duration).SetEase(ease).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}

	public void MoveToBySpeed(Vector2 position, float moveSpeed, Ease ease = Ease.Linear, Action callback = null)
	{
		setEnabled(value: false);
		float duration = Vector2.Distance(base.transform.position, position) / moveSpeed;
		MoveToByDuration(position, duration, ease, callback);
	}

	public void ForceSetPosition(Vector2 position, bool constraint = false)
	{
		if (constraint)
		{
			position = Constraint(scenePosition, sceneSize, position);
		}
		base.transform.position = new Vector3(position.x, position.y, base.transform.position.z);
	}

	public void RefreshResolution()
	{
		if (DolocAPI.IsGameInitialized)
		{
			SetResolution(DolocAPI.worldResolution.x, DolocAPI.worldResolution.y);
		}
	}

	public void SetResolution(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			Debug.LogError("invalid resolution : " + width + "x" + height);
			return;
		}
		float num = (float)width / (float)height;
		float num2 = DolocAPI.mainCamera.orthographicSize * 2f;
		float x = num2 * num;
		camSize = new Vector2(x, num2);
		SetRoomRange(scenePosition, sceneSize);
	}

	private void SetResolution_ScaleCanvas(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			Debug.LogError("invalid resolution : " + width + "x" + height);
			return;
		}
		camSize = new Vector2(width, height) * (1f / 32f);
		Camera.main.orthographicSize = camSize.y * 0.5f;
		SetRoomRange(scenePosition, sceneSize);
	}

	public void SetRoomRange(Vector2 pos, Vector2 size)
	{
		scenePosition = pos;
		sceneSize = size;
		base.enabled = true;
		if (size == camSize)
		{
			base.enabled = false;
		}
		else
		{
			Vector2 vector = camSize / 2f;
			Vector2 vector2 = pos + vector;
			Vector2 vector3 = pos + size - vector;
			xRange = new Vector2(vector2.x, vector3.x);
			yRange = new Vector2(vector2.y, vector3.y);
			if (Mathf.Abs(xRange.x - xRange.y) < 0.05f)
			{
				moveFunc = moveY;
			}
			else if (Mathf.Abs(yRange.x - yRange.y) < 0.05f)
			{
				moveFunc = moveX;
			}
			else
			{
				moveFunc = moveXY;
			}
		}
		_isNotFixedRange = base.enabled;
	}

	public void SetPosition(Vector2 agentPos)
	{
		if (base.enabled)
		{
			Vector3 vector = new Vector3(Mathf.Clamp(agentPos.x, xRange.x, xRange.y), Mathf.Clamp(agentPos.y, yRange.x, yRange.y), base.transform.position.z);
			base.transform.position = vector;
		}
		else
		{
			Vector3 vector2 = new Vector3(scenePosition.x + camSize.x / 2f, scenePosition.y + camSize.y / 2f, base.transform.position.z);
			base.transform.position = vector2;
		}
	}

	public Vector3 Constraint(Vector2 scenePos, Vector2 sceneSize, Vector2 position)
	{
		if (sceneSize == camSize)
		{
			Vector2 vector = scenePos + camSize * 0.5f;
			return new Vector3(vector.x, vector.y, base.transform.position.z);
		}
		Vector2 vector2 = camSize * 0.5f;
		Vector2 vector3 = scenePos + vector2;
		Vector2 vector4 = scenePos + sceneSize - vector2;
		Vector2 vector5 = new Vector2(Mathf.Clamp(position.x, vector3.x, vector4.x), Mathf.Clamp(position.y, vector3.y, vector4.y));
		return new Vector3(vector5.x, vector5.y, base.transform.position.z);
	}

	private void MoveXDirect(float dt)
	{
		Vector3 vector = follow.position;
		Vector3 vector2 = base.transform.position;
		vector2 = new Vector3(vector.x, vector.y, vector2.z);
		vector2 = new Vector3(Mathf.Clamp(vector2.x, xRange.x, xRange.y), vector2.y, vector2.z);
		base.transform.position = vector2;
	}

	public void MoveYDirect(float dt)
	{
		Vector3 vector = follow.position;
		Vector3 vector2 = base.transform.position;
		vector2 = new Vector3(vector.x, vector.y, vector2.z);
		vector2 = new Vector3(vector2.x, Mathf.Clamp(vector2.y, yRange.x, yRange.y), vector2.z);
		base.transform.position = vector2;
	}

	public void MoveXYDirect(float dt)
	{
		Vector3 vector = follow.position;
		Vector3 vector2 = base.transform.position;
		vector2 = new Vector3(vector.x, vector.y, vector2.z);
		vector2 = new Vector3(Mathf.Clamp(vector2.x, xRange.x, xRange.y), Mathf.Clamp(vector2.y, yRange.x, yRange.y), vector2.z);
		base.transform.position = vector2;
	}

	private void moveX(float dt)
	{
		Vector3 zero = Vector3.zero;
		float horizontalError = HorizontalError;
		if (Mathf.Abs(horizontalError) >= threshold)
		{
			zero.x += horizontalError * dt * currentMoveSpeed;
		}
		Transform obj = base.transform;
		obj.Translate(zero);
		Vector3 vector = obj.position;
		vector = new Vector3(Mathf.Clamp(vector.x, xRange.x, xRange.y), vector.y, vector.z);
		obj.position = vector;
	}

	private void moveY(float dt)
	{
		Vector3 zero = Vector3.zero;
		float verticalError = VerticalError;
		if (Mathf.Abs(verticalError) >= threshold)
		{
			zero.y += verticalError * dt * currentMoveSpeed;
		}
		Transform obj = base.transform;
		obj.Translate(zero);
		Vector3 vector = obj.position;
		vector = new Vector3(vector.x, Mathf.Clamp(vector.y, yRange.x, yRange.y), vector.z);
		obj.position = vector;
	}

	private void moveXY(float dt)
	{
		Vector3 zero = Vector3.zero;
		Vector3 vector = new Vector3(HorizontalError, VerticalError, 0f);
		if (vector.magnitude >= threshold)
		{
			zero += vector * (dt * currentMoveSpeed);
		}
		Transform obj = base.transform;
		obj.Translate(zero);
		Vector3 vector2 = obj.position;
		vector2 = new Vector3(Mathf.Clamp(vector2.x, xRange.x, xRange.y), Mathf.Clamp(vector2.y, yRange.x, yRange.y), vector2.z);
		obj.position = vector2;
	}
}
