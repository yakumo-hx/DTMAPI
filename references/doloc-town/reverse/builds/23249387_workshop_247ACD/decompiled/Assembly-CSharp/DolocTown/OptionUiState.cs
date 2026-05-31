using System;
using DolocTown.UI;

namespace DolocTown;

public class OptionUiState : DolocUiState<DialogueOptionPanel>
{
	private string[] titles;

	private Action<string> onConfirm;

	public override bool ShowFlowPoints => false;

	public bool HandleStartUpArgs(string[] titles, Action<string> onConfirm)
	{
		this.titles = titles;
		this.onConfirm = onConfirm;
		return true;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
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
		base.panel.Render(titles);
		base.panel.SetClickCallbacks(delegate(int index)
		{
			gameController.PopState();
			onConfirm?.Invoke(titles[index]);
		});
	}

	protected override void Unregister()
	{
		base.panel.RemoveCallbacks();
	}
}
