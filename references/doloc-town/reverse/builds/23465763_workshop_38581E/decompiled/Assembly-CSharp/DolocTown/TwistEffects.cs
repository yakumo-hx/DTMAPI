using System;
using DG.Tweening;
using DolocTown.Rendering;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(RenderTextureUVHandler))]
[GameEffectsManager("spec/twist", DolocGameAssets.GAME_ENTITY_TWIST, Frequency = 5)]
public class TwistEffects : GameEntity
{
	private Tween _tween;

	private Material _materialClone;

	private Vector2 _offset;

	public Action<GameEntity> Recycle { get; set; }

	protected override void __Init()
	{
		base.__Init();
		_materialClone = new Material(GetComponent<SpriteRenderer>().sharedMaterial);
		GetComponent<SpriteRenderer>().sharedMaterial = _materialClone;
		GetComponent<RenderTextureUVHandler>().InitRenderInfo(DolocAPI.worldResolution);
		Vector2 vector = GetComponent<SpriteRenderer>().bounds.size;
		_offset = vector * -0.5f;
	}

	public override void OnReuse()
	{
		base.OnReuse();
		_tween?.Kill();
		_tween = null;
		_materialClone.SetFloat("_ShockWaveProcess", 0f);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		_tween?.Kill();
		_tween = null;
	}

	public void Raise(Vector2 ws, float dur)
	{
		base.transform.position = ws + _offset;
		_tween = _materialClone.DOFloat(1f, "_ShockWaveProcess", dur).SetEase(Ease.OutExpo).OnComplete(delegate
		{
			_tween?.Kill();
			Recycle?.Invoke(this);
		});
	}

	private void OnDestroy()
	{
		if (_materialClone != null)
		{
			UnityEngine.Object.Destroy(_materialClone);
		}
	}
}
