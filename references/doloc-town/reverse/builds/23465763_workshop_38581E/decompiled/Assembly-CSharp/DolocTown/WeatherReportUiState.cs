using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class WeatherReportUiState : DolocUiState<WeatherReportPanel>
{
	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Register()
	{
	}

	protected override void Unregister()
	{
	}

	protected override void Show()
	{
		base.Show();
		base.panel.RefreshAndShow();
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}
}
