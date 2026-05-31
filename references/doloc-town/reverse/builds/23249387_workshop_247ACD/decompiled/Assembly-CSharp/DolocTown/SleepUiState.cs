using System;
using DolocTown.Config.Settings;
using DolocTown.GameData;
using DolocTown.UI;

namespace DolocTown;

public class SleepUiState : DolocUiState<DialogueOptionPanel>
{
	private Action onSleepEnd;

	public override bool ShowFlowPoints => false;

	private GlobalParameterInfo globalParameter => DolocAPI.GlobalParameter;

	private DateInfo timeInfo => DolocAPI.archiveHandle.DateNow;

	public bool HandleSleepStartUpArgs(Action onSleepEnd)
	{
		this.onSleepEnd = onSleepEnd;
		return true;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed && !base.panel.SelectThenFireClickExitOption())
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		DolocAPI.SetPPM_CinemaScreen(value: true);
		base.panel.Show();
		base.panel.Select(0);
	}

	protected override void Hide()
	{
		DolocAPI.SetPPM_CinemaScreen(value: false);
		base.panel.Hide();
		base.Hide();
	}

	protected override void Register()
	{
		base.panel.RenderWithExitOption(GetOptions());
		base.panel.SetClickCallbacks(delegate(int index)
		{
			gameController.PopState();
			HandleSleepOption(index);
		});
	}

	protected override void Unregister()
	{
		base.panel.RemoveCallbacks();
	}

	private string[] GetOptions()
	{
		return new string[4]
		{
			base.staticTexts.UiSavepointSleep,
			base.staticTexts.UiSavepointNap.Format(globalParameter.NapHour),
			base.staticTexts.UiSavepointDefault,
			base.staticTexts.UiSavepointCancel
		};
	}

	private void HandleSleepOption(int index)
	{
		switch (index)
		{
		case 0:
			SleepToFixedClock(globalParameter.WakeUpClock);
			break;
		case 1:
		{
			int totalSeconds = globalParameter.GameHours2Secs(globalParameter.NapHour);
			FallAsleep(totalSeconds, DolocAPI.userSettings.saveOnNap, recover: true, fullSleepBuff: false, isNap: true);
			break;
		}
		case 2:
			SaveGameOnly();
			break;
		}
	}

	private void SaveGameOnly()
	{
		DolocAPI.SaveGame(DolocAPI.archiveHandle.archiveIndex);
	}

	private void SleepToFixedClock(int clock)
	{
		if (clock <= timeInfo.Hour)
		{
			clock += globalParameter.Day2Hour;
		}
		int num = clock - timeInfo.Hour - 1;
		int num2 = globalParameter.Hour2Min - timeInfo.Minute;
		int totalSeconds = (num * globalParameter.Hour2Min + num2) * globalParameter.TULength / globalParameter.TU2Min;
		FallAsleep(totalSeconds, saveData: true, recover: true, fullSleepBuff: true, isNap: false);
	}

	private void FallAsleep(int totalSeconds, bool saveData, bool recover, bool fullSleepBuff, bool isNap)
	{
		if (totalSeconds <= 0)
		{
			return;
		}
		DolocAPI.archiveHandle.farmData.agentData.OnSpiritReset();
		DolocAPI.agent.MotionAbility.ClearEnvModerate();
		float num = DolocAPI.GlobalParameter.FadeDefaultDurationOnSleep / 2f;
		DolocAPI.archiveHandle.PassTime(totalSeconds, delegate
		{
			if (DolocAPI.CurrentRoom.Type == RoomType.Farm)
			{
				((TemplateRoom)DolocAPI.CurrentRoom).RefreshRender();
			}
			if (recover)
			{
				DolocAPI.RecoverPlayerValue(totalSeconds, fullSleepBuff, isNap);
			}
			int value = DolocAPI.GlobalParameter.Secs2GameHour(totalSeconds);
			DolocAPI.BroadcastInt(GameEventType.SLEEP, value);
			onSleepEnd?.Invoke();
			DolocAPI.OnWakeUp(saveData, sendEvent: true, clearRecoveryDecayBuffs: true);
		}, num, num);
	}
}
