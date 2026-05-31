using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class FadeUpTip : DolocRecyclableObject
{
	[SerializeField]
	private Ease ease = Ease.OutExpo;

	[SerializeField]
	private float distance = 6f;

	[SerializeField]
	private float duration = 1f;

	public Action<FadeUpTip> recycle { get; set; }

	public void RaiseUp(Vector2 pos, Sprite icon, Ease ease, float duration, float popDistance, bool flipX)
	{
		SetVisible(value: true);
		GetComponent<SpriteRenderer>().sprite = icon;
		position = new Vector3(pos.x, pos.y, position.z);
		Sequence sequence = DOTween.Sequence();
		sequence.Join(base.transform.DOLocalMove(new Vector3(pos.x, pos.y + popDistance, position.z), duration).SetEase(ease));
		SpriteRenderer sp = GetComponent<SpriteRenderer>();
		sp.color = Color.white;
		sp.flipX = flipX;
		sequence.Join(DOTween.ToAlpha(() => sp.color, delegate(Color color)
		{
			sp.color = color;
		}, 0f, duration));
		sequence.OnComplete(delegate
		{
			recycle?.Invoke(this);
		});
	}

	public void MoveTo(Vector2 from, Vector2 to, Sprite icon, Ease ease, float duration)
	{
		SetVisible(value: true);
		GetComponent<SpriteRenderer>().sprite = icon;
		position = new Vector3(from.x, from.y, position.z);
		Sequence sequence = DOTween.Sequence();
		sequence.Join(base.transform.DOMove(new Vector3(to.x, to.y, position.z), duration).SetEase(ease));
		SpriteRenderer sp = GetComponent<SpriteRenderer>();
		sp.color = Color.white;
		sequence.Join(DOTween.ToAlpha(() => sp.color, delegate(Color color)
		{
			sp.color = color;
		}, 0f, duration));
		sequence.OnComplete(delegate
		{
			recycle?.Invoke(this);
		});
	}

	public void RaiseDown(Vector2 pos, Sprite icon, Ease ease, float duration, float popDistance)
	{
		SetVisible(value: true);
		GetComponent<SpriteRenderer>().sprite = icon;
		position = new Vector3(pos.x, pos.y + popDistance, position.z);
		Sequence sequence = DOTween.Sequence();
		sequence.Join(base.transform.DOLocalMove(new Vector3(pos.x, pos.y, position.z), duration).SetEase(ease));
		SpriteRenderer sp = GetComponent<SpriteRenderer>();
		sp.color = DolocColor.empty;
		sequence.Join(DOTween.ToAlpha(() => sp.color, delegate(Color color)
		{
			sp.color = color;
		}, 1f, duration));
		sequence.OnComplete(delegate
		{
			recycle?.Invoke(this);
		});
	}

	public void test()
	{
		Init();
		RaiseUp(new Vector2(30f, 7f), LocSprites.UI_INFOICON_STAR3, ease, duration, distance, flipX: false);
	}
}
