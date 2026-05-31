using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class OperationTipMulti : DolocUiRecyclableObject
{
	[SerializeField]
	private HorizontalLayoutGroup _layoutGroup;

	[SerializeField]
	private CanvasGroup _group;

	[SerializeField]
	private Image _icon;

	[SerializeField]
	private Text _prompt;

	[SerializeField]
	private float _largeWidth = 360f;

	[SerializeField]
	private float _largeHeight = 72f;

	[SerializeField]
	private Font _largeFont;

	[SerializeField]
	private int _largeFontSize = 24;

	[SerializeField]
	private float _smallWidth = 295f;

	[SerializeField]
	private float _smallHeight = 48f;

	[SerializeField]
	private Font _smallFont;

	[SerializeField]
	private int _smallFontSize = 20;

	[SerializeField]
	private float _duration = 0.75f;

	[SerializeField]
	private Ease _shapeEase = Ease.OutExpo;

	[SerializeField]
	private Ease _positionEase = Ease.OutExpo;

	[SerializeField]
	private float _hideDistance = 36f;

	[SerializeField]
	private float _partInDistance = 150f;

	[SerializeField]
	private float _pushDistance = 6f;

	[SerializeField]
	private float _pushDuration = 0.1f;

	[SerializeField]
	private Ease _pushEase = Ease.OutExpo;

	[SerializeField]
	private float _resumeDuration = 0.1f;

	[SerializeField]
	private Ease _resumeEase = Ease.OutBack;

	private Tween _tween;

	private Tween _partInTween;

	private Tween _pushAnim;

	public Action Callback { get; set; }

	public Sprite KeyIcon
	{
		get
		{
			return _icon.sprite;
		}
		set
		{
			_icon.sprite = value;
		}
	}

	public string Prompt
	{
		get
		{
			return _prompt.text;
		}
		set
		{
			_prompt.text = value;
		}
	}

	public void Clone(OperationTipMulti tip)
	{
		KeyIcon = tip.KeyIcon;
		Prompt = tip.Prompt;
	}

	public void Remove()
	{
		Callback?.Invoke();
		Callback = null;
	}

	public float GetTextWidth(float fullWidth)
	{
		return fullWidth - _icon.rectTransform.sizeDelta.x - _layoutGroup.spacing - (float)_layoutGroup.padding.horizontal;
	}

	public void SetSize(float width, float height)
	{
		RectTransform obj = (RectTransform)base.transform;
		Vector2 sizeDelta = obj.sizeDelta;
		sizeDelta.x = width;
		sizeDelta.y = height;
		obj.sizeDelta = sizeDelta;
	}

	private void JoinTweenSize(Sequence sequence, float width, float height, float duration, Ease ease)
	{
		sequence.Join(base.rectTransform.DOSizeDelta(new Vector2(width, height), duration).SetEase(ease));
		RectTransform rectTransform = _prompt.rectTransform;
		sequence.Join(DOTweenModuleUI.DOSizeDelta(endValue: new Vector2(GetTextWidth(width), rectTransform.sizeDelta.y), target: _prompt.rectTransform, duration: duration).SetEase(ease));
	}

	private void KillAll()
	{
		_tween?.Kill();
		_partInTween?.Kill();
		_pushAnim?.Kill();
	}

	private void _MoveY(float yPosition, float width, float height, float duration, Ease shapeEase, Ease positionEase, Font font, int fontSize, bool shouldTweenAlpha = false, float alpha = 1f)
	{
		KillAll();
		_prompt.font = font;
		_prompt.fontSize = fontSize;
		Sequence sequence = DOTween.Sequence();
		JoinTweenSize(sequence, width, height, duration, shapeEase);
		sequence.Join(base.rectTransform.DOLocalMoveY(yPosition, duration).SetEase(positionEase));
		if (shouldTweenAlpha)
		{
			sequence.Join(_group.DOFade(alpha, duration));
		}
		_tween = sequence;
	}

	private void _MoveYToHide(float yPosition, float duration, Ease positionEase)
	{
		KillAll();
		base.positionLocal = Vector2.zero;
		_MoveY(yPosition, _smallWidth, _smallHeight, _duration, _shapeEase, _positionEase, _smallFont, _smallFontSize, shouldTweenAlpha: true, 0f);
	}

	public void MoveFromRight()
	{
		KillAll();
		_prompt.font = _largeFont;
		_prompt.fontSize = _largeFontSize;
		_prompt.rectTransform.sizeDelta = new Vector2(_largeWidth, _largeFontSize);
		SetSize(_largeWidth, _largeHeight);
		base.positionLocal = new Vector2(_partInDistance, 0f);
		_group.alpha = 0f;
		Sequence sequence = DOTween.Sequence();
		sequence.Join(_group.DOFade(1f, _duration));
		sequence.Join(base.rectTransform.DOLocalMoveX(0f, _duration).SetEase(_positionEase));
		_partInTween = sequence;
	}

	public void MoveUp()
	{
		_group.alpha = 1f;
		base.positionLocal = Vector2.zero;
		SetSize(_largeWidth, _largeHeight);
		_MoveY(_largeHeight, _smallWidth, _smallHeight, _duration, _shapeEase, _positionEase, _smallFont, _smallFontSize);
	}

	public void MoveUpToHide()
	{
		_MoveYToHide(_largeHeight + _hideDistance, _duration, _positionEase);
	}

	public void MoveFromBottom()
	{
		if (_partInTween != null)
		{
			_partInTween.Kill();
			base.positionLocalX = 0f;
		}
		((RectTransform)base.transform).localPosition = new Vector2(0f, 0f - _largeHeight - _hideDistance);
		_group.alpha = 0f;
		_MoveY(0f - _largeHeight, _smallWidth, _smallHeight, _duration, _shapeEase, _positionEase, _smallFont, _smallFontSize, shouldTweenAlpha: true);
	}

	public void MoveFromTop()
	{
		if (_partInTween != null)
		{
			_partInTween.Kill();
			base.positionLocalX = 0f;
		}
		((RectTransform)base.transform).localPosition = new Vector2(0f, _largeHeight + _hideDistance);
		_group.alpha = 0f;
		_MoveY(_largeHeight, _smallWidth, _smallHeight, _duration, _shapeEase, _positionEase, _smallFont, _smallFontSize, shouldTweenAlpha: true);
	}

	public void MoveCenter()
	{
		if (_partInTween != null)
		{
			_partInTween.Kill();
			base.positionLocalX = 0f;
			_group.alpha = 1f;
		}
		SetSize(_smallWidth, _smallHeight);
		_MoveY(0f, _largeWidth, _largeHeight, _duration, _shapeEase, _positionEase, _largeFont, _largeFontSize);
	}

	public void MoveDown()
	{
		_group.alpha = 1f;
		base.positionLocal = Vector2.zero;
		SetSize(_largeWidth, _largeHeight);
		_MoveY(0f - _largeHeight, _smallWidth, _smallHeight, _duration, _shapeEase, _positionEase, _smallFont, _smallFontSize);
	}

	public void MoveDownToHide()
	{
		_MoveYToHide(0f - _largeHeight - _hideDistance, _duration, _positionEase);
	}

	public void PushHorizontal()
	{
		_pushAnim?.Kill();
		base.positionLocal = Vector2.zero;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(base.rectTransform.DOLocalMoveX(0f - _pushDistance, _pushDuration).SetEase(_pushEase));
		sequence.Append(base.rectTransform.DOLocalMoveX(0f, _resumeDuration).SetEase(_resumeEase));
		sequence.OnComplete(delegate
		{
			_pushAnim = null;
		});
		_pushAnim = sequence;
	}

	public void PushVertical()
	{
		_pushAnim?.Kill();
		base.positionLocal = Vector2.zero;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(base.rectTransform.DOLocalMoveY(0f - _pushDistance, _pushDuration).SetEase(_pushEase));
		sequence.Append(base.rectTransform.DOLocalMoveY(0f, _resumeDuration).SetEase(_resumeEase));
		sequence.OnComplete(delegate
		{
			_pushAnim = null;
		});
		_pushAnim = sequence;
	}
}
