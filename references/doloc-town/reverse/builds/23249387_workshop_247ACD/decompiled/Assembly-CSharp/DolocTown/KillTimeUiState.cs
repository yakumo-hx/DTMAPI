using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class KillTimeUiState : DolocUiState<DialogueOptionPanel>
{
	private KillTimeState killTimeState;

	private List<string> options = new List<string>();

	private List<Action> optionHandlers = new List<Action>();

	private Action<float> handleKillTime;

	private Action onEnd;

	public override bool ShowFlowPoints => false;

	public bool HandleKillTimeStartUpArgs(KillTimeState killTimeState, Action<float> handleKillTime, Action onEnd)
	{
		this.killTimeState = killTimeState;
		this.handleKillTime = handleKillTime;
		this.onEnd = onEnd;
		SitParams sitParams = this.killTimeState.SitParams;
		options.Clear();
		optionHandlers.Clear();
		float timeScale = sitParams.timeScale;
		if (timeScale > 1f)
		{
			string arg = ((timeScale % 1f == 0f) ? $"{timeScale:F0}" : $"{timeScale:F1}");
			options.Add(DolocUtils.Format(DolocConfig.StaticTexts.UiSavepointKillTime, arg));
			optionHandlers.Add(delegate
			{
				handleKillTime?.Invoke(timeScale);
			});
		}
		if (sitParams.save)
		{
			options.Add(DolocConfig.StaticTexts.UiSavepointDefault);
			optionHandlers.Add(delegate
			{
				DolocAPI.SaveGame(DolocAPI.archiveHandle.archiveIndex);
				handleKillTime?.Invoke(0f);
			});
		}
		if (options.Count > 0)
		{
			options.Insert(0, DolocConfig.StaticTexts.UiSavepointSit);
			optionHandlers.Insert(0, delegate
			{
				handleKillTime?.Invoke(1f);
				DolocAPI.SetPPM_CinemaScreen(value: false);
			});
			options.Add(DolocConfig.StaticTexts.UiSavepointExit);
			optionHandlers.Add(delegate
			{
				handleKillTime?.Invoke(0f);
			});
		}
		if (options.Count == 0)
		{
			handleKillTime?.Invoke(1f);
			onEnd?.Invoke();
		}
		return options.Count > 0;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed && !base.panel.SelectThenFireClickExitOption())
		{
			gameController.PopState();
			handleKillTime?.Invoke(1f);
			onEnd?.Invoke();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		base.panel.Select(0);
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}

	protected override void Register()
	{
		base.panel.RenderWithExitOption(options.ToArray());
		base.panel.SetClickCallbacks(delegate(int index)
		{
			gameController.PopState();
			HandleKillTimeOption(index);
			onEnd?.Invoke();
		});
	}

	protected override void Unregister()
	{
		base.panel.RemoveCallbacks();
	}

	private void HandleKillTimeOption(int index)
	{
		if (optionHandlers.Count != 0)
		{
			index = Mathf.Clamp(index, 0, optionHandlers.Count);
			optionHandlers[index]?.Invoke();
		}
	}
}
