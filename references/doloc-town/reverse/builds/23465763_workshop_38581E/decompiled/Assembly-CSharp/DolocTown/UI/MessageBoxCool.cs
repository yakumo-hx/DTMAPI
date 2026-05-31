using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class MessageBoxCool : DolocObject
{
	[SerializeField]
	private Image imageBackground;

	[SerializeField]
	[Range(0f, 1f)]
	private float imageBackgroundScaleTime;

	[SerializeField]
	[Range(0f, 1f)]
	private float imageBackgroundScaleTimeExit;

	[SerializeField]
	private TMP_Text textInfo;

	[SerializeField]
	private Ease imageBackgroundScaleEase;

	[SerializeField]
	private Ease textInfoEnterEase;

	[SerializeField]
	private Ease textInfoExitEase;

	private Tween _tween;

	private readonly Queue<(string, float)> _messageQueue = new Queue<(string, float)>();

	protected override void __Init()
	{
		base.__Init();
		SetVisible(value: false);
	}

	public void Show(string msg, float holdTime = 2f)
	{
		if (_tween != null)
		{
			if (_messageQueue.Count < 5)
			{
				_messageQueue.Enqueue((msg, holdTime));
			}
		}
		else
		{
			_Show(msg, holdTime);
		}
	}

	private void _Show(string message, float holdTime)
	{
		SetVisible(value: true);
		_tween?.Kill();
		textInfo.text = message;
		textInfo.color = textInfo.color.Alpha(0f);
		Vector3 vector = textInfo.rectTransform.position;
		vector.x = -960f;
		textInfo.rectTransform.position = vector;
		imageBackground.fillOrigin = 0;
		imageBackground.fillAmount = 0f;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(imageBackground.DOFillAmount(1f, imageBackgroundScaleTime).SetEase(imageBackgroundScaleEase).OnComplete(delegate
		{
			imageBackground.fillOrigin = 1;
		}));
		sequence.Join(textInfo.DOFade(1f, 0.5f));
		sequence.Join(textInfo.rectTransform.DOLocalMoveX(0f, 0.5f).SetEase(textInfoEnterEase));
		sequence.AppendInterval(holdTime);
		sequence.Append(textInfo.DOFade(0f, 0.5f));
		sequence.Join(textInfo.rectTransform.DOLocalMoveX(960f, 0.5f).SetEase(textInfoExitEase));
		sequence.Join(imageBackground.DOFillAmount(0f, imageBackgroundScaleTimeExit));
		sequence.OnComplete(delegate
		{
			if (_messageQueue.Count > 0)
			{
				_tween = null;
				var (message2, holdTime2) = _messageQueue.Dequeue();
				_Show(message2, holdTime2);
			}
			else
			{
				SetVisible(value: false);
				_tween = null;
			}
		});
		_tween = sequence;
	}
}
