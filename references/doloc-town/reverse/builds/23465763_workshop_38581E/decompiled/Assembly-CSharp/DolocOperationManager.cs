using DolocTown.UI;
using UnityEngine;

public class DolocOperationManager : DolocUiEntity
{
	private OperationTipPanel tipPanel;

	protected override void __Init()
	{
		base.__Init();
		tipPanel = GetComponentInChildren<OperationTipPanel>();
		tipPanel.Init();
		tipPanel.SetVisible(value: false);
		base.gameObject.SetActive(value: true);
	}

	public void RenderTipPanelOnly(TipPanelText tipPanelText, bool hide = true)
	{
		if (tipPanelText.keys != null)
		{
			tipPanel.Render(tipPanelText.keys, tipPanelText.contents, hide);
		}
	}

	public void ShowTipPanel(TipPanelText tipPanelText)
	{
		if (tipPanelText.keys != null)
		{
			tipPanel.Show(tipPanelText.keys, tipPanelText.contents);
		}
	}

	public void ShowTipPanel(Vector2 localPos, TipPanelText tipPanelText)
	{
		if (tipPanelText.keys != null)
		{
			tipPanel.Show(localPos, tipPanelText.keys, tipPanelText.contents);
		}
	}

	public void HideTipPanel()
	{
		tipPanel.Hide().Forget();
	}
}
