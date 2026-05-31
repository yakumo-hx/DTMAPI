using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class TutorialPanelUiState : DolocUiState<TutorialPanel>
{
	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	public bool HandleStartUpArgs(string prefabName)
	{
		DolocAPI.archiveHandle.UnlockTutorial(prefabName);
		return base.panel.LoadPage(prefabName);
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			ClosePanel();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.SetMode(TutorialPanel.TutorialPanelMode.Single);
		base.panel.Show();
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	private void ClosePanel()
	{
		gameController.PopState();
	}

	protected override void Register()
	{
	}

	protected override void Unregister()
	{
	}
}
