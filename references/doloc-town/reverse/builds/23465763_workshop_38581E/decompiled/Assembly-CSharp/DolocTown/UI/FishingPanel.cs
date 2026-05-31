using UnityEngine;

namespace DolocTown.UI;

public class FishingPanel : DolocUIPanel
{
	[SerializeField]
	private FishingNoteBar noteBar;

	[SerializeField]
	private FishingProgressBar progressBar;

	[SerializeField]
	private FishingTurntable turntable;

	protected override void __Init()
	{
		base.__Init();
		noteBar.Init();
		progressBar.Init();
		turntable.Init();
		noteBar.width = (float)DolocAPI.GlobalParameter.FishingNoteBarWidthPixel * 4f;
		progressBar.width = (float)DolocAPI.GlobalParameter.FishingProgressBarWidthPixel * 4f;
	}

	public void StartSpawn(FishingNoteSpawner noteSpawner)
	{
		noteBar.gameObject.SetActive(value: true);
		noteBar.StartSpawn(noteSpawner);
		turntable.StopBonusSpeedUp();
		turntable.InitPlayAnimation();
	}

	public void SetProgress(float progress)
	{
		progressBar.SetProgress(progress);
	}

	public void PlayReelAnimation(bool reelIn)
	{
		turntable.PlayReelAnimation(reelIn);
	}

	public void PlayBonusSpeedUp()
	{
		turntable.PlayBonusSpeedUp();
	}

	public void StopBonusSpeedUp()
	{
		turntable.StopBonusSpeedUp();
	}

	public void PlaySparkAnimation()
	{
		turntable.PlaySparkAnimation();
	}

	public void PlayPunishAnimation(float duration)
	{
		turntable.PlayPunishAnimation(duration);
	}

	public void SetPunishEffectVisible(bool value)
	{
		noteBar.SetPunishEffectVisible(value);
	}

	public void SetHoldEffectVisible(bool value)
	{
		noteBar.SetHoldEffectVisible(value);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		noteBar.StopSpawn();
		turntable.SetSparkVisible(value: false);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_FISHING_REEL);
	}

	protected override void OnFinishHide()
	{
		base.OnFinishHide();
		noteBar.RecycleAllNotes();
	}
}
