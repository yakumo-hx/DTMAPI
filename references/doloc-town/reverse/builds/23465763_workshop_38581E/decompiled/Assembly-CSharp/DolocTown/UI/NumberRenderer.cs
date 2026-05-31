using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class NumberRenderer : DolocUiObject
{
	[SerializeField]
	private Image numberImg;

	[SerializeField]
	private Image numberImgFake;

	private Sequence seq;

	[SerializeField]
	[Range(0f, 1f)]
	private float animDuration = 0.5f;

	public Sprite sprite
	{
		set
		{
			numberImg.sprite = value;
			numberImgFake.sprite = value;
		}
	}

	public void SetNumber(Sprite numberSprite, float duration = 0.5f, Ease ease = Ease.OutExpo)
	{
		if (!(numberSprite == null) && !(numberSprite == numberImg.sprite))
		{
			seq?.Kill();
			seq = DOTween.Sequence();
			float num = numberImg.rectTransform.sizeDelta.y / 2f;
			numberImgFake.sprite = numberImg.sprite;
			numberImgFake.transform.localPosition = Vector3.zero;
			numberImgFake.color = Color.white;
			numberImg.transform.localPosition = new Vector3(0f, 0f - num, 0f);
			numberImg.color = new Color(1f, 1f, 1f, 0f);
			numberImg.sprite = numberSprite;
			seq.Join(numberImg.rectTransform.DOLocalMoveY(0f, duration).SetEase(ease));
			seq.Join(numberImg.DOFade(1f, duration));
			seq.Join(numberImgFake.rectTransform.DOLocalMoveY(num, duration).SetEase(ease));
			seq.Join(numberImgFake.DOFade(0f, duration));
			seq.OnComplete(delegate
			{
				seq = null;
			});
		}
	}

	public void Test(Sprite sprite)
	{
		SetNumber(sprite, animDuration);
	}
}
