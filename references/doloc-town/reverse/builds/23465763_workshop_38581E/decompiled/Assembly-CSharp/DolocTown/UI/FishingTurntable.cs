using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class FishingTurntable : DolocUiObject
{
	[SerializeField]
	private Animator handleAnimator;

	[SerializeField]
	private Animator sparkAnimator;

	[SerializeField]
	private float reelInSpeed = 1f;

	[SerializeField]
	private float reelOutSpeed = 0.6f;

	[SerializeField]
	private float bonusSpeed = 5f;

	[SerializeField]
	private float shakeStrength = 8f;

	[SerializeField]
	private int shakeVibrato = 30;

	private bool isReelIn;

	private bool inBonusDuration;

	protected override void __Init()
	{
		base.__Init();
		SetSparkVisible(value: false);
	}

	public void InitPlayAnimation()
	{
		isReelIn = false;
		handleAnimator.speed = 0f;
	}

	public void PlayReelAnimation(bool reelIn)
	{
		if (isReelIn != reelIn)
		{
			DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_FISHING_REEL);
			DolocAPI.Sound.PostSoundEvent(reelIn ? SoundEvents.PLAY_FISHING_REEL_IN : SoundEvents.PLAY_FISHING_REEL_OUT);
			isReelIn = reelIn;
			float speed = (reelIn ? reelInSpeed : reelOutSpeed);
			ContinuePlayAnimation(reelIn ? "ReelIn" : "ReelOut", speed);
		}
	}

	private void ContinuePlayAnimation(string animationName, float speed)
	{
		AnimatorStateInfo currentAnimatorStateInfo = handleAnimator.GetCurrentAnimatorStateInfo(0);
		if (!currentAnimatorStateInfo.IsName(animationName))
		{
			float normalizedTime = 1f - currentAnimatorStateInfo.normalizedTime % 1f;
			handleAnimator.Play(animationName, 0, normalizedTime);
			handleAnimator.speed = speed;
		}
	}

	public void PlayBonusSpeedUp()
	{
		handleAnimator.speed = bonusSpeed;
	}

	public void StopBonusSpeedUp()
	{
		handleAnimator.speed = (isReelIn ? reelInSpeed : reelOutSpeed);
	}

	public void PlayPunishAnimation(float duration)
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_FISHING_ERROR_HINT);
		base.rectTransform.DOShakeAnchorPos(duration, shakeStrength, shakeVibrato, 360f);
	}

	public void PlaySparkAnimation()
	{
		SetSparkVisible(value: true);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_FISHING_BONUS_HINT);
		sparkAnimator.Play("FishingSpark", 0, 0f);
	}

	public void SetSparkVisible(bool value)
	{
		sparkAnimator.gameObject.SetActive(value);
	}
}
