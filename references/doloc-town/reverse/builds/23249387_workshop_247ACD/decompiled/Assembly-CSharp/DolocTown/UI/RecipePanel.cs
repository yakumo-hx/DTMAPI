using UnityEngine;

namespace DolocTown.UI;

public class RecipePanel : CraftPanel<RecipeSlot, RecipeViewer, RecipeData>
{
	[SerializeField]
	public SwitchBar switchBar;

	protected override void __Init()
	{
		base.__Init();
		switchBar.Init();
	}

	public void SetTaskInfo(string text)
	{
		viewer.SetTaskInfo(text ?? string.Empty);
	}
}
