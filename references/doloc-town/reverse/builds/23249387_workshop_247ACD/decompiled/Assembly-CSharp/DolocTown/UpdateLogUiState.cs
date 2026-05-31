using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class UpdateLogUiState : DolocUiState<UpdateLogPanel>
{
	protected override bool RevertPrevSelected => true;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override void OnInit()
	{
		base.OnInit();
		base.panel.SetText(UpdateLogHelper.GetUpdateLog());
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	protected override void Register()
	{
	}

	protected override void Unregister()
	{
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}
}
