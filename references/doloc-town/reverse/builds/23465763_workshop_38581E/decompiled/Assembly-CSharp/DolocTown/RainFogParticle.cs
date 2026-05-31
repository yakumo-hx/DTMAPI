using DG.Tweening;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[GameEffectsManager("/spec/rainfogs", DolocGameAssets.GAME_ENTITY_RAINFOG)]
[RequireComponent(typeof(SpriteRenderer))]
public class RainFogParticle : GameEntity
{
	[SerializeField]
	private Sprite[] sprites;

	[SerializeField]
	private Color colorStart;

	[SerializeField]
	private Color colorEnd;

	private Tween _tween;

	private Vector2 _moveDir;

	public void Invoke(float moveSpeed, float fadeOutDur = 3f, float fadeInDur = 10f)
	{
		_tween?.Kill();
		_moveDir = ((Random.value > 0.5f) ? Vector2.right : Vector2.left) * moveSpeed;
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (!sprites.IsNullOrEmpty())
		{
			component.sprite = sprites.Choice();
		}
		float value = Random.value;
		component.color = new Color(Mathf.Lerp(colorStart.r, colorEnd.r, value), Mathf.Lerp(colorStart.g, colorEnd.g, value), Mathf.Lerp(colorStart.b, colorEnd.b, value), 0f);
		Sequence sequence = DOTween.Sequence();
		sequence.Join(component.DOFade(new Vector2(colorStart.a, colorEnd.a).Between(), fadeOutDur));
		sequence.AppendInterval(Random.Range(1f, 3f));
		sequence.Append(component.DOFade(0f, fadeInDur));
		sequence.OnComplete(delegate
		{
			DolocAPI.effectProvider.RecycleEntityEffect(this);
		});
		_tween = sequence;
	}

	public void Invoke(float moveSpeed, Color color, float fadeOutDur = 3f, float fadeInDur = 10f)
	{
		_tween?.Kill();
		_moveDir = ((Random.value > 0.5f) ? Vector2.right : Vector2.left) * moveSpeed;
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (!sprites.IsNullOrEmpty())
		{
			component.sprite = sprites.Choice();
		}
		component.color = color;
		Sequence sequence = DOTween.Sequence();
		sequence.Join(component.DOFade(new Vector2(colorStart.a, colorEnd.a).Between(), fadeOutDur));
		sequence.AppendInterval(Random.Range(1f, 3f));
		sequence.Append(component.DOFade(0f, fadeInDur));
		sequence.OnComplete(delegate
		{
			DolocAPI.effectProvider.RecycleEntityEffect(this);
		});
		_tween = sequence;
	}

	public void FixedUpdate()
	{
		base.transform.Translate(_moveDir * Time.fixedDeltaTime);
	}
}
