using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class LevelTipRenderer : DolocUiEntity
{
	[SerializeField]
	private DoubleNumberRenderer levelRenderer;

	[SerializeField]
	private Image iconImg;

	[SerializeField]
	private Image expImg;

	private Sequence sequence;

	private Queue<Tuple<Sprite, int>> queue = new Queue<Tuple<Sprite, int>>();

	protected override void __Init()
	{
		base.__Init();
		levelRenderer.Init();
	}

	private void Update()
	{
		base.transform.position = DolocAPI.WorldToScreen(DolocAPI.agent.PositionHeadTop);
	}

	public void Raise(Sprite icon, int to, float expDuration = 0.5f, Ease expEase = Ease.OutCirc, float durationNumber = 0.5f, Ease numberEase = Ease.OutBounce)
	{
		if (sequence != null)
		{
			queue.Enqueue(new Tuple<Sprite, int>(icon, to));
			return;
		}
		SetVisible(value: true);
		levelRenderer.SetNumber(to - 1);
		iconImg.sprite = icon;
		expImg.fillAmount = 0f;
		sequence = DOTween.Sequence();
		sequence.Join(expImg.DOFillAmount(1f, expDuration).SetEase(expEase).OnComplete(delegate
		{
			levelRenderer.TweenNumber(to, durationNumber, numberEase);
		}));
		sequence.AppendInterval(durationNumber + 1f);
		sequence.OnComplete(delegate
		{
			sequence = null;
			if (queue.Count > 0)
			{
				Tuple<Sprite, int> tuple = queue.Dequeue();
				Raise(tuple.Item1, tuple.Item2, expDuration, expEase, durationNumber, numberEase);
			}
			else
			{
				SetVisible(value: false);
			}
		});
	}
}
