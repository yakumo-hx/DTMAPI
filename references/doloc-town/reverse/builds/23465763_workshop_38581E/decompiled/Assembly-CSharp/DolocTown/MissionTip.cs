using System;
using DG.Tweening;
using DolocTown.UI;
using TMPro;
using UnityEngine;

namespace DolocTown;

public class MissionTip : DolocUiRecyclableObject
{
	[SerializeField]
	private TMP_Text title;

	[SerializeField]
	private TMP_Text guide;

	[SerializeField]
	private TMP_Text progress;

	[SerializeField]
	private float showTime = 0.3f;

	[SerializeField]
	private float popDistance = 100f;

	private DolocTweenLocalMove localMove;

	protected override void __Init()
	{
		base.__Init();
		localMove = new DolocTweenLocalMove(base.transform, Ease.OutExpo, showTime);
	}

	public void Render(string title, string guide, string progress, string progressColor = "#00FFA5")
	{
		this.title.text = title;
		this.guide.text = guide;
		this.progress.text = " <color=" + progressColor + ">" + progress + "</color>";
	}

	public void ShowAtCurrent()
	{
		Vector2 vector = base.positionLocal;
		base.positionLocal = new Vector2(vector.x, vector.y - popDistance);
		localMove.forcePlay(vector);
	}

	public void Hide(Action callback)
	{
		Vector2 vector = new Vector2(base.positionLocal.x + 400f, base.positionLocal.y);
		localMove.forcePlay(vector);
		DOVirtual.DelayedCall(showTime, delegate
		{
			SetVisible(value: false);
			callback?.Invoke();
		});
	}

	public void MoveToTargetPos(Vector2 target)
	{
		localMove.forcePlay(target);
	}
}
