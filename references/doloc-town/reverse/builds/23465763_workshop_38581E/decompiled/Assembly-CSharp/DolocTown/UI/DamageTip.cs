using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DamageTip : DolocUiRecyclableObject
{
	[SerializeField]
	private Color heavyColor = Color.red;

	[SerializeField]
	private Vector2 randomRange = new Vector2(-1f, 1f);

	private TMP_Text text;

	private Image background;

	private Sequence seq;

	private Vector2 ws;

	public Action<DamageTip> recycleHandle { get; set; }

	protected override void __Init()
	{
		base.__Init();
		background = GetComponent<Image>();
		text = GetComponentInChildren<TMP_Text>(includeInactive: true);
	}

	private void LateUpdate()
	{
		Vector2 vector = DolocAPI.WorldToScreen(ws);
		base.transform.position = new Vector3(vector.x, base.transform.position.y, base.transform.position.z);
	}

	private void ResetText(int value)
	{
		text.text = value.ToString();
		text.color = Color.white;
	}

	private void ResetPosition(Vector2 ws)
	{
		ws += new Vector2(UnityEngine.Random.Range(randomRange.x, randomRange.y), UnityEngine.Random.Range(randomRange.x, randomRange.y));
		this.ws = ws;
		base.transform.position = DolocAPI.WorldToScreen(ws);
	}

	public void RaiseHeavy(int value, Vector2 ws, float duration, float popDistance, float waitTime)
	{
		seq?.Kill();
		ResetText(value);
		ResetPosition(ws);
		background.color = heavyColor;
		seq = DOTween.Sequence();
		seq.AppendInterval(waitTime * 0.75f);
		seq.OnComplete(delegate
		{
			seq = null;
			recycleHandle?.Invoke(this);
		});
	}

	public void Raise(int count, Vector2 ws, float duration, float popDistance, float waitTime = 1.5f)
	{
		seq?.Kill();
		ResetText(count);
		ResetPosition(ws);
		background.color = Color.clear;
		seq = DOTween.Sequence();
		seq.Join(base.transform.DOLocalMoveY(base.transform.localPosition.y + popDistance, duration).SetEase(Ease.OutExpo));
		seq.AppendInterval(waitTime);
		seq.Join(DOTween.ToAlpha(() => text.color, delegate(Color c)
		{
			text.color = c;
		}, 0f, 0.7f));
		seq.OnComplete(delegate
		{
			seq = null;
			recycleHandle?.Invoke(this);
		});
	}

	public void Test()
	{
		if (!base.isInitialized)
		{
			Init();
		}
		Raise(100, new Vector2(300f, 300f), 0.7f, 50f, 1f);
	}
}
