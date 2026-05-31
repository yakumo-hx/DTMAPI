using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DynamicArrow : DolocUiObject
{
	[SerializeField]
	private float offset = 20f;

	[SerializeField]
	private float duration = 0.8f;

	[SerializeField]
	private bool isHorizontal = true;

	private Image icon;

	private float src;

	private float dst;

	private Tween tween;

	protected override void __Init()
	{
		base.__Init();
		icon = GetComponent<Image>();
		src = (isHorizontal ? base.positionLocal.x : base.positionLocal.y);
		dst = src + offset;
		DolocUtils.setAlpha(icon, 0f);
		SetVisible(value: false);
	}

	private void OnEnable()
	{
		Init();
		tween?.Kill();
		DolocUtils.setAlpha(icon, 1f);
		Ping();
	}

	private void OnDisable()
	{
		Init();
		tween?.Kill();
		DolocUtils.setAlpha(icon, 0f);
	}

	private void Ping()
	{
		tween = (isHorizontal ? icon.transform.DOLocalMoveX(dst, duration / 2f).SetUpdate(isIndependentUpdate: true).OnComplete(Pong) : icon.transform.DOLocalMoveY(dst, duration / 2f).SetUpdate(isIndependentUpdate: true).OnComplete(Pong));
	}

	private void Pong()
	{
		tween = (isHorizontal ? icon.transform.DOLocalMoveX(src, duration / 2f).SetUpdate(isIndependentUpdate: true).OnComplete(Ping) : icon.transform.DOLocalMoveY(src, duration / 2f).SetUpdate(isIndependentUpdate: true).OnComplete(Ping));
	}
}
