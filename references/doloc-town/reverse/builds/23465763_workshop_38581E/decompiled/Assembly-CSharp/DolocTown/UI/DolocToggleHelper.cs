using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DolocToggleHelper : DolocUiObject
{
	[SerializeField]
	private Toggle toggle;

	[SerializeField]
	private Color hdlDeep;

	[SerializeField]
	private Color hdlLight;

	[SerializeField]
	private Color bgDeep;

	[SerializeField]
	private Color bgLight;

	[SerializeField]
	private float colorDuration = 0.1f;

	[SerializeField]
	private float moveDuration = 0.1f;

	[SerializeField]
	private Image handleSprite;

	[SerializeField]
	private Image backgroundSprite;

	[SerializeField]
	private RectTransform handleArea;

	private float localOffsetX;

	private Vector2 hdlLocalPosOn;

	private Vector2 hdlLocalPosOff;

	protected override void __Init()
	{
		base.__Init();
		localOffsetX = (handleArea.rect.width - handleSprite.rectTransform.rect.width) / 2f;
	}

	public void SetToggleState(bool value)
	{
		handleSprite.DOColor(value ? hdlLight : hdlDeep, colorDuration);
		backgroundSprite.DOColor(value ? bgLight : bgDeep, colorDuration);
		handleSprite.rectTransform.DOLocalMoveX(value ? localOffsetX : (0f - localOffsetX), moveDuration);
	}
}
