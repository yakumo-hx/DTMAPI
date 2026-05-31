using UnityEngine;

namespace DolocTown.UI;

public class AutomateBotWidget : DolocUIPanel
{
	[SerializeField]
	private AutomateBotSubMenu automateBotSubMenu;

	[SerializeField]
	private AutomateBotViewer viewer;

	public AutomateBotSubMenu AutomateBotSubMenu => automateBotSubMenu;

	public AutomateBotViewer Viewer => viewer;

	public BotRecipeViewer BotRecipeViewer => viewer.RecipeViewer;

	public OptionItem OptionItem1 => Viewer.OptionItemUI1;

	public OptionItem OptionItem2 => Viewer.OptionItemUI2;

	protected override void __Init()
	{
		base.__Init();
		viewer.Init();
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		automateBotSubMenu.Show();
		Viewer.Show();
		viewer.RecipeViewer.Show();
	}
}
