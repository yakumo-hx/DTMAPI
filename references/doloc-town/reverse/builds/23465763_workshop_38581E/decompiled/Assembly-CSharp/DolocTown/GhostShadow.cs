using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[GameEffectsManager("/anims", DolocGameAssets.GAME_ENTITY_GHOST_SHADOW)]
public class GhostShadow : GameEntity
{
	[SerializeField]
	private float _duration = 0.5f;

	private SpriteRenderer _spriteRenderer;

	private Tween _tween;

	private Vector3 _directionScale;

	public Action<GhostShadow> Recycle { get; set; }

	protected override void __Init()
	{
		base.__Init();
		_spriteRenderer = GetComponent<SpriteRenderer>();
	}

	public void Invoke(Vector3 positionWS, Vector2 direction, Sprite sprite, bool useScale = false, float targetScale = 3f)
	{
		_tween?.Kill();
		base.transform.position = positionWS;
		_directionScale = new Vector3(direction.x, 1f, 1f);
		base.transform.localScale = _directionScale;
		_spriteRenderer.sprite = sprite;
		_spriteRenderer.color = _spriteRenderer.color.Alpha(1f);
		Sequence sequence = DOTween.Sequence();
		sequence.Join(_spriteRenderer.DOFade(0f, _duration));
		if (useScale)
		{
			sequence.Join(base.transform.DOScale(_directionScale * targetScale, _duration).SetEase(Ease.OutExpo));
		}
		sequence.OnComplete(delegate
		{
			_tween = null;
			Recycle(this);
		});
		_tween = sequence;
	}
}
