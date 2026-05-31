using DG.Tweening;
using DolocTown.Config;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MoneyTip : DolocBasicTip
{
	[SerializeField]
	private float showTime = 0.2f;

	private Text text;

	private int sourceValue;

	private int destValue;

	private Vector2 showPosition;

	private bool isShowing;

	private Tween currentAnimation;

	protected int currentError;

	private float errorScale;

	protected override void __Init()
	{
		base.__Init();
		isShowing = true;
		sourceValue = 0;
		currentAnimation = DOTween.To(() => errorScale, delegate(float v)
		{
			errorScale = v;
		}, 1f, showTime).SetEase(Ease.OutExpo).OnUpdate(UpdateText)
			.OnComplete(OnComplete);
		currentAnimation.SetAutoKill(autoKillOnCompletion: false);
		currentAnimation.Pause();
		text = GetComponentInChildren<Text>();
		showPosition = base.transform.localPosition;
	}

	protected override void OnHover()
	{
		this.HoverTextSmall(DolocConfig.StaticTexts.ItemMoneyTitle, UIAlignmentType.BottomMiddle, UIAlignmentType.TopMiddle);
	}

	public virtual void SetMoney(int dest, bool useAnimation = true)
	{
		destValue = dest;
		if (!useAnimation)
		{
			sourceValue = dest;
			text.text = dest + " G";
			return;
		}
		currentError = dest - sourceValue;
		if (currentError != 0)
		{
			errorScale = 0f;
			currentAnimation.Restart();
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_GOLD_ROLL);
		}
	}

	private void UpdateText()
	{
		text.text = (int)((float)currentError * errorScale) + sourceValue + " G";
	}

	private void OnComplete()
	{
		sourceValue = destValue;
		text.text = destValue + " G";
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_UI_GOLD_ROLL);
	}
}
