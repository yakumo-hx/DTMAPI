using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class DialogueHistoryUiState : DolocUiState<DialogueHistoryPanel>
{
	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private DialogueHistoryManager historyManager => DolocAPI.archiveHandle.cityData.dialogueManager.historyManager;

	private IScrollContentRect _contentRect => base.panel.scrollContentRect;

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
		if (userInput.BaseScrollDir.magnitude > 0f)
		{
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Render(historyManager.GetCurrentHistoryData());
		base.panel.Show();
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}
}
