using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using DolocTown.UI;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class FishingGameScrollBar : FishingGameController
{
	private GameStatus currentGameStatus;

	private FishingPanel panel;

	private ItemFishingRod fishingRod;

	private FishInfo fishProto;

	private FishingNoteSpawner noteSpawner;

	private float startTime;

	private float currentTime;

	private int fishStamina;

	private RSTimer animationTimer = new RSTimer();

	private float punishDuration = DolocAPI.GlobalParameter.FishingPunishAnimationDuration;

	private float startPunishTime = DolocAPI.GlobalParameter.FishingPunishStartTime;

	private float noPunishDuration;

	private int currentNoteIndex;

	private FishingNoteData currentNote;

	private float currentScore;

	private bool inBonusDuration;

	private int comboCount;

	private float bonusMultiplier;

	private float catchMultiplier;

	private float escapeMultiplier;

	private float struggleMultiplier;

	private float lastPunishTime;

	public override GameStatus CurrentGameStatus => currentGameStatus;

	public sealed override bool IsValid { get; protected set; }

	private ItemFunctionFishingRod rodFunction => fishingRod?.proto?.Function as ItemFunctionFishingRod;

	public bool ReelIn { get; private set; }

	private float animationInterval => DolocAPI.GlobalParameter.FishingAnimationMinimumInterval;

	private bool IsFishingKeyPushed
	{
		get
		{
			if (!DolocAPI.UserInput.NormalUseTool && !DolocAPI.UserInput.NormalUseToolInProgress && !DolocAPI.UserInput.NormalFishing && !DolocAPI.UserInput.NormalFishingInProgress && !DolocAPI.UserInput.NormalUseItem)
			{
				return DolocAPI.UserInput.NormalUseItemInProgress;
			}
			return true;
		}
	}

	public override void StartGame(ItemFishingRod fishingRod, FishInfo fishProto)
	{
		panel = DolocAPI.uiSystem.GetEntity<FishingPanel>();
		this.fishingRod = fishingRod;
		this.fishProto = fishProto;
		if (panel == null || rodFunction == null || fishProto == null)
		{
			IsValid = false;
			return;
		}
		noteSpawner = new FishingNoteSpawner(fishProto);
		IsValid = true;
		ReelIn = false;
		fishStamina = Mathf.Max(1, fishProto.MaxStamina.RandomCount);
		currentGameStatus = GameStatus.Running;
		startTime = Time.time;
		currentNoteIndex = 0;
		currentNote = null;
		currentScore = (float)fishStamina * rodFunction.InitProgress;
		inBonusDuration = false;
		comboCount = 0;
		bonusMultiplier = fishProto.BonusMultiplier * rodFunction.BonusMultiplier;
		catchMultiplier = rodFunction.CatchSpeed * fishProto.CatchMultiplier;
		escapeMultiplier = (float)fishProto.EscapeSpeed * rodFunction.AntiEscapeMultiplier;
		struggleMultiplier = (float)fishProto.EscapeSpeed * fishProto.StruggleMultiplier * rodFunction.AntiStruggleMultiplier;
		animationTimer.SetInterval(animationInterval);
		lastPunishTime = 0f;
		noPunishDuration = punishDuration / 2f;
		panel.Show();
		panel.StartSpawn(noteSpawner);
		RefreshProgress();
	}

	private void RefreshProgress()
	{
		float progress = Mathf.Clamp(currentScore / (float)fishStamina, 0f, 1f);
		panel.SetProgress(progress);
	}

	public override void UpdateGame(float dt)
	{
		currentTime = Time.time - startTime;
		if (currentTime < noteSpawner.delayTime)
		{
			return;
		}
		if (currentScore <= 0f)
		{
			currentGameStatus = GameStatus.Failed;
			return;
		}
		if (currentScore >= (float)fishStamina)
		{
			currentGameStatus = GameStatus.Success;
			return;
		}
		bool flag = false;
		bool holdEffectVisible = false;
		bool punishEffectVisible = false;
		if (currentNote == null || currentTime > currentNote.endTime)
		{
			currentNote = noteSpawner.GetNote(currentNoteIndex++);
			if (currentNote == null || currentNote.noteType == FishingNoteType.Delay)
			{
				return;
			}
			comboCount = 0;
			inBonusDuration = currentNote.noteType == FishingNoteType.Bonus;
		}
		if ((DolocAPI.UserInput.NormalUseTool || DolocAPI.UserInput.NormalUseItem || DolocAPI.UserInput.NormalFishing) && currentTime >= currentNote.startTime && inBonusDuration)
		{
			flag = true;
			if (comboCount++ == 0)
			{
				currentScore += (float)fishProto.BonusBaseScore * bonusMultiplier;
				panel.PlayBonusSpeedUp();
				panel.PlaySparkAnimation();
			}
			else
			{
				comboCount++;
				currentScore += (float)fishProto.BonusExtraScore * bonusMultiplier;
			}
		}
		if (IsFishingKeyPushed)
		{
			ReelIn = true;
			if (currentTime >= currentNote.startTime)
			{
				if (!inBonusDuration)
				{
					holdEffectVisible = true;
					currentScore += catchMultiplier * dt;
				}
			}
			else if (currentTime > startPunishTime)
			{
				bool flag2 = !inBonusDuration && currentTime >= currentNote.startTime - noPunishDuration;
				if (Time.time - lastPunishTime >= punishDuration && !flag2)
				{
					panel.PlayPunishAnimation(punishDuration);
					lastPunishTime = Time.time;
				}
				currentScore -= struggleMultiplier * dt;
				punishEffectVisible = true;
			}
		}
		else
		{
			ReelIn = inBonusDuration && comboCount > 0;
			if (!ReelIn)
			{
				currentScore -= escapeMultiplier * dt;
			}
		}
		panel.SetHoldEffectVisible(holdEffectVisible);
		panel.SetPunishEffectVisible(punishEffectVisible);
		if (flag)
		{
			panel.PlayReelAnimation(ReelIn);
			panel.PlayBonusSpeedUp();
			animationTimer.Reset();
		}
		else if (animationTimer.Tick(dt))
		{
			panel.PlayReelAnimation(ReelIn);
			panel.StopBonusSpeedUp();
		}
		RefreshProgress();
	}

	public override void FixedUpdateGame(float dt)
	{
	}

	public override void StopGame()
	{
		panel.Hide();
	}
}
