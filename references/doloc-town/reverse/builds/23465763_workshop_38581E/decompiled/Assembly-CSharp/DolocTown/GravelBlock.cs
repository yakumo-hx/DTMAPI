using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DolocTown.Config.Tile;
using UnityEngine;

namespace DolocTown;

public class GravelBlock : InteractableObjectExclude, ITileMaterial
{
	private Collider2D blockCollider;

	private SpriteRenderer spriteRenderer;

	private bool inLoop;

	public TileMaterial tileMaterial => TileMaterial.STONE;

	protected override ITouchCheckStrategy touchChecker { get; set; }

	private GravelBlockTouchChecker checker => touchChecker as GravelBlockTouchChecker;

	private float lastDuration => DolocAPI.GlobalParameter.GravelBlockLastDuration;

	private float refreshDuration => DolocAPI.GlobalParameter.GravelBlockRefreshDuration;

	protected override void __Init()
	{
		base.__Init();
		spriteRenderer = GetComponent<SpriteRenderer>();
		blockCollider = GetComponent<Collider2D>();
		touchChecker = new GravelBlockTouchChecker();
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (!inLoop)
		{
			if (checker.groundPassBy)
			{
				Hide().Forget();
			}
			else
			{
				OnStay().Forget();
			}
		}
	}

	private async UniTaskVoid OnStay()
	{
		await UniTask.WaitUntil(() => checker.isClimb || !base.isTouched || inLoop);
		if (checker.isClimb)
		{
			Hide(delegate
			{
				checker.player.Drop();
			}).Forget();
		}
	}

	private async UniTaskVoid Hide(Action callback = null)
	{
		inLoop = true;
		await UniTask.Delay((int)(lastDuration * 1000f));
		int step = 5;
		float dur = 0.1f;
		for (int i = 0; i < step; i++)
		{
			spriteRenderer.DOFade((i % 2 == 0) ? 0f : 1f, dur);
			await UniTask.Delay((int)(dur * 1000f));
		}
		blockCollider.gameObject.SetActive(value: false);
		callback?.Invoke();
		await UniTask.Delay((int)(refreshDuration * 1000f));
		spriteRenderer.DOFade(0f, 0f);
		Show().Forget();
	}

	private async UniTaskVoid Show()
	{
		spriteRenderer.DOFade(1f, 0.1f);
		await UniTask.Delay(100);
		blockCollider.gameObject.SetActive(value: true);
		inLoop = false;
	}
}
