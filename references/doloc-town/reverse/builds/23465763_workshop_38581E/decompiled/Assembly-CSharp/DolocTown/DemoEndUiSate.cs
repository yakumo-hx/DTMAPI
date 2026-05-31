using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class DemoEndUiSate : DolocUiState<DemoEndBox>
{
	protected override bool RevertPrevSelected => true;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		base.panel.buttonWishlist.Select();
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed && base.panel.CanCancel())
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
}
