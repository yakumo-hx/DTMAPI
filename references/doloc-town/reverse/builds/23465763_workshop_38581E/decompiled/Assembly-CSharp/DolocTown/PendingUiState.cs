using DolocTown.UI;
using UnityEngine.EventSystems;

namespace DolocTown;

public class PendingUiState : DolocUiState<PendingBox>
{
	protected override bool RevertPrevSelected => true;

	public bool HandleStartUpArgs(string text)
	{
		base.panel.Render(text);
		return true;
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
		base.panel.Show();
		EventSystem.current.SetSelectedGameObject(null);
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}
}
