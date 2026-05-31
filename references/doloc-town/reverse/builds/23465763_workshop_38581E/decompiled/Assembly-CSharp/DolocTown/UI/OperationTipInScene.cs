using System;
using DG.Tweening;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown.UI;

public class OperationTipInScene : OperationTipBase
{
	[SerializeField]
	[Range(0.1f, 1f)]
	private float pushDuration = 0.25f;

	[SerializeField]
	[Range(0.1f, 1f)]
	private float resumeDuration = 0.15f;

	[SerializeField]
	[Range(0.1f, 1f)]
	private float fastPushDuration = 0.15f;

	[SerializeField]
	[Range(0.1f, 1f)]
	private float fastResumeDuration = 0.1f;

	[SerializeField]
	private Ease pushEase = Ease.OutExpo;

	[SerializeField]
	private Ease resumeEase = Ease.OutBack;

	[SerializeField]
	private float pushOffset = 25f;

	[SerializeField]
	private float fadeOutDuration = 0.25f;

	private bool isMovingTarget;

	private Func<Vector2> movingTargetPositionGetter;

	private Vector2 targetPosition;

	private Tween currentTween;

	private Tween pushAnim;

	private Vector2 TargetPosition
	{
		get
		{
			if (isMovingTarget)
			{
				try
				{
					return movingTargetPositionGetter();
				}
				catch (Exception)
				{
					isMovingTarget = false;
					targetPosition = new Vector2(-1000f, -1000f);
					return targetPosition;
				}
			}
			return targetPosition;
		}
	}

	private void LateUpdate()
	{
		Vector2 vector = DolocAPI.WorldToScreen(TargetPosition);
		if (pushAnim != null)
		{
			base.positionX = vector.x;
		}
		else
		{
			base.position = vector;
		}
	}

	private void Update()
	{
		if (pushAnim == null)
		{
			base.position = DolocAPI.WorldToScreen(TargetPosition);
		}
	}

	private void ShowAt(Vector2 positionWS)
	{
		targetPosition = positionWS;
		currentTween?.Kill();
		currentTween = DOTween.To(() => base.Alpha, delegate(float value)
		{
			base.Alpha = value;
		}, 1f, 0.25f);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		currentTween?.Kill();
		pushAnim?.Kill();
	}

	public void Show(Vector2 positionWS, string prompt, Sprite key)
	{
		pushAnim?.Kill();
		pushAnim = null;
		base.Alpha = 0f;
		SetVisible(value: true);
		isMovingTarget = false;
		Render(key, prompt, delegate
		{
			ShowAt(positionWS);
		});
	}

	public void Show(Func<Vector2> positionGetter, string prompt, Sprite key)
	{
		if (pushAnim != null)
		{
			pushAnim.Kill();
			pushAnim = null;
		}
		base.Alpha = 0f;
		SetVisible(value: true);
		movingTargetPositionGetter = positionGetter;
		isMovingTarget = true;
		Render(key, prompt, delegate
		{
			ShowAt(positionGetter());
		});
	}

	public void PushFast()
	{
		pushAnim?.Kill();
		Vector3 vector = DolocAPI.WorldToScreen(TargetPosition);
		Sequence sequence = DOTween.Sequence();
		sequence.Append(base.transform.DOMoveY(vector.y - pushOffset, fastPushDuration).SetEase(pushEase));
		sequence.Append(base.transform.DOMoveY(vector.y, fastResumeDuration).SetEase(resumeEase));
		sequence.OnComplete(delegate
		{
			pushAnim = null;
		});
		pushAnim = sequence;
	}

	public void Push(Action callback = null)
	{
		pushAnim?.Kill();
		Vector3 vector = DolocAPI.WorldToScreen(TargetPosition);
		Sequence sequence = DOTween.Sequence();
		sequence.Append(base.transform.DOMoveY(vector.y - pushOffset, pushDuration).SetEase(pushEase));
		sequence.Append(base.transform.DOMoveY(vector.y, resumeDuration).SetEase(resumeEase));
		sequence.OnComplete(delegate
		{
			pushAnim = null;
			callback?.Invoke();
		});
		pushAnim = sequence;
	}

	public void Hide(Action callback = null)
	{
		currentTween?.Kill();
		currentTween = DOTween.To(() => base.Alpha, delegate(float value)
		{
			base.Alpha = value;
		}, 0f, fadeOutDuration).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}

	protected override void __Init()
	{
		base.__Init();
		Tables.LanguageChange = (Action)Delegate.Combine(Tables.LanguageChange, new Action(Refresh));
	}

	private void OnDestroy()
	{
		Tables.LanguageChange = (Action)Delegate.Remove(Tables.LanguageChange, new Action(Refresh));
	}

	private void Refresh()
	{
		Hide();
	}
}
