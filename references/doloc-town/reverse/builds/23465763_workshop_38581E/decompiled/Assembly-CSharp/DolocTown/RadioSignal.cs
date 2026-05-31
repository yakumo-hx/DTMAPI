using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class RadioSignal : InstantGoEffects
{
	[SerializeField]
	private float shootDuration = 0.5f;

	[SerializeField]
	private Ease shootEase = Ease.Linear;

	[SerializeField]
	private float shootDistance = 45f;

	[SerializeField]
	private float waitDuration = 1f;

	[SerializeField]
	private float disappearDistance = 5f;

	[SerializeField]
	private float disappearDuration = 0.5f;

	[SerializeField]
	private Ease disappearEase = Ease.Linear;

	private LineRenderer _lineRenderer;

	private Tween _tween;

	private LineRenderer LineRenderer
	{
		get
		{
			if (_lineRenderer == null)
			{
				_lineRenderer = base.gameObject.GetComponent<LineRenderer>();
			}
			return _lineRenderer;
		}
	}

	private Vector2 LineStartPos
	{
		get
		{
			return LineRenderer.GetPosition(0);
		}
		set
		{
			LineRenderer.SetPosition(0, value);
		}
	}

	private Vector2 LineEndPos
	{
		get
		{
			return LineRenderer.GetPosition(1);
		}
		set
		{
			LineRenderer.SetPosition(1, value);
		}
	}

	private float Distance
	{
		get
		{
			return LineEndPos.y - LineStartPos.y;
		}
		set
		{
			LineEndPos = new Vector2(LineStartPos.x, LineStartPos.y + value);
		}
	}

	private float Width
	{
		get
		{
			return LineRenderer.startWidth;
		}
		set
		{
			LineRenderer.startWidth = value;
			LineRenderer.endWidth = value;
		}
	}

	public override void Raise()
	{
	}

	public void RadioSignalRaise(Vector2 startPosition, Action callback = null)
	{
		_tween?.Kill();
		Sequence sequence = DOTween.Sequence();
		LineStartPos = startPosition;
		Width = 0f;
		Distance = 0f;
		sequence.Append(DOTween.To(() => Distance, delegate(float v)
		{
			Distance = v;
		}, shootDistance, shootDuration).SetEase(shootEase));
		sequence.Join(DOTween.To(() => Width, delegate(float v)
		{
			Width = v;
		}, 1f, shootDuration).SetEase(shootEase));
		sequence.AppendInterval(waitDuration);
		sequence.Join(DOTween.To(() => Width, delegate(float v)
		{
			Width = v;
		}, 0f, disappearDuration).SetEase(disappearEase));
		sequence.OnComplete(delegate
		{
			_tween = null;
			callback?.Invoke();
		});
		_tween = sequence;
	}
}
