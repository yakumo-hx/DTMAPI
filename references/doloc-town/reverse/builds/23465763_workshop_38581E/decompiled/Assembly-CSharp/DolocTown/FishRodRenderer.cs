using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class FishRodRenderer : DolocObject
{
	[SerializeField]
	private Transform castUiOffset;

	[SerializeField]
	private Transform _fishRodEndPoint;

	[SerializeField]
	private Vector2 _hookCastDirectionVec;

	[SerializeField]
	private Vector2 _castForceRange = new Vector2(15f, 42f);

	[SerializeField]
	private Gradient _castForceColor;

	[SerializeField]
	private Sprite _hookSprite;

	[SerializeField]
	private float pullHeight = 3f;

	[SerializeField]
	[Tooltip("拉杆动画结束后，额外等待多久时间才能进入下一个状态")]
	private float _extraAnimationDuration = 0.25f;

	[SerializeField]
	[Tooltip("取消拉杆时，额外等待多久时间才能进入下一个状态")]
	private float _cancelPullDuration = 0.25f;

	private readonly HashSet<string> fishrodAnimationClips = new HashSet<string>();

	private FishRodHook _hook;

	private FishRodLine _fishrodLine;

	private Animator _animator;

	private float _castPowerProgress;

	private bool _lockHookOnRope;

	private bool _isReadyNow;

	private bool isFaceRight;

	private Action<FishingPool> _onHooked;

	public Vector2 CastProgressBarPosition => castUiOffset.position;

	public Vector2 CastProgressBarPositionScreen => DolocAPI.WorldToScreen(CastProgressBarPosition);

	public FishRodLine Line => _fishrodLine;

	public Vector2 FishHookPosition => _hook.position2d;

	public Vector2 FishHookPositionScreen => DolocAPI.WorldToScreen(_hook.position2d);

	public FishRodHook Hook => _hook;

	protected override void __Init()
	{
		base.__Init();
		_animator = GetComponent<Animator>();
		AnimationClip[] animationClips = _animator.runtimeAnimatorController.animationClips;
		foreach (AnimationClip animationClip in animationClips)
		{
			fishrodAnimationClips.Add(animationClip.name);
		}
		_fishrodLine = GetComponentInChildren<FishRodLine>(includeInactive: true);
		_fishrodLine.Init();
		_hook = GetComponentInChildren<FishRodHook>(includeInactive: true);
		_hook.Init();
		_hook.OnHooked += OnHooked;
		SetVisible(value: false);
	}

	public void HideHook()
	{
		_hook.SetVisible(value: false);
		_fishrodLine.gameObject.SetActive(value: false);
	}

	public void EnableFishShadow()
	{
		_hook.EnableFishShadow(_fishRodEndPoint.position);
	}

	public void HideFishShadow()
	{
		_hook.HideFishShadow();
	}

	public void StopBattle()
	{
		_hook.DisableBattle();
		_hook.DisableFloating();
	}

	public void SetPower(float value)
	{
		_castPowerProgress = Mathf.Clamp01(value);
	}

	public Color GetCastForceColor(float value)
	{
		return _castForceColor.Evaluate(value);
	}

	public void SetCastInfo(bool isFaceRight, Action<FishingPool> onHooked)
	{
		this.isFaceRight = isFaceRight;
		_onHooked = onHooked;
	}

	public void ReadyHook()
	{
		_hook.DisableBattle();
		_hook.DisableFloating();
		_hook.RBEnabled = false;
		_hook.HookSprite = _hookSprite;
		_hook.SetVisible(value: true);
		_hook.transform.position = _fishRodEndPoint.position;
		_fishrodLine.SetVisible(value: false);
		_isReadyNow = true;
	}

	public void CastHook()
	{
		_isReadyNow = false;
		_lockHookOnRope = false;
		_hook.ResetHook();
		_hook.SetVisible(value: true);
		Vector2 vector = (isFaceRight ? _hookCastDirectionVec.normalized : new Vector2(0f - _hookCastDirectionVec.x, _hookCastDirectionVec.y).normalized);
		float num = Mathf.Lerp(_castForceRange.x, _castForceRange.y, _castPowerProgress);
		_hook.AddForce(vector * num);
		_fishrodLine.UseStraightLine();
	}

	public float PullCancel()
	{
		_hook.DisableFloating();
		_hook.DisableBattle();
		_hook.RBEnabled = false;
		_hook.HookSprite = _hookSprite;
		_fishrodLine.UseRopeLine(doubleSide: false, useShortLine: true);
		_lockHookOnRope = true;
		_isReadyNow = false;
		return _cancelPullDuration;
	}

	public float Pull(Vector2 agentPosition)
	{
		_fishrodLine.UseRopeLine(doubleSide: true, useShortLine: true);
		_hook.DisableFloating();
		_hook.DisableBattle();
		_hook.RBEnabled = true;
		_lockHookOnRope = false;
		_isReadyNow = false;
		float t;
		Vector2 velocity = CalcVelocity(_hook.GetComponent<Rigidbody2D>(), agentPosition, pullHeight, out t);
		StartCoroutine(WaitForNextFrame(delegate
		{
			_hook.Velocity = velocity;
		}));
		return t + _extraAnimationDuration;
	}

	private Vector2 CalcVelocity(Rigidbody2D rb, Vector2 bodyPosition, float dropHeight, out float t)
	{
		float num = (0f - rb.gravityScale) * Physics2D.gravity.y;
		float num2 = 1f / num;
		Vector2 vector = rb.transform.position;
		float num3 = bodyPosition.y - vector.y;
		float num4 = bodyPosition.x - vector.x;
		if (num3 > 0f)
		{
			float num5 = Mathf.Abs(num3);
			float num6 = Mathf.Sqrt(dropHeight * num2) * 1.4142135f;
			float num7 = Mathf.Sqrt((num5 + dropHeight) * num2) * 2f;
			t = num7 + num6;
			return new Vector2(num4 / t, num * num7);
		}
		float num8 = Mathf.Abs(num3);
		t = Mathf.Sqrt(num8 * num2) * 1.4142135f;
		t = Mathf.Max(t, 0.5f);
		return new Vector2(num4 / t, 30f);
	}

	private IEnumerator WaitForNextFrame(Action callback)
	{
		yield return new WaitForEndOfFrame();
		callback?.Invoke();
	}

	private void OnHooked(FishingPool pool)
	{
		_fishrodLine.UseRopeLine();
		_onHooked?.Invoke(pool);
	}

	public void Play(string toolName, string behaviourName)
	{
		if (!base.isVisible)
		{
			SetVisible(value: true);
		}
		string text = toolName + "_" + behaviourName;
		if (fishrodAnimationClips.Contains(text))
		{
			_animator.Play(text, 0, 0f);
			return;
		}
		Debug.LogWarning("未配置鱼竿动画\"" + text + "\"");
		if (!fishrodAnimationClips.Contains(behaviourName))
		{
			Debug.LogError("鱼竿fallback动画\"" + behaviourName + "\"丢失");
		}
		_animator.Play(behaviourName, 0, 0f);
	}

	private void Update()
	{
		if (_lockHookOnRope)
		{
			_hook.position2d = _fishrodLine.RopeEndPosition;
		}
		if (_isReadyNow)
		{
			_hook.position2d = _fishRodEndPoint.position;
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(castUiOffset.position, 0.1f);
	}
}
