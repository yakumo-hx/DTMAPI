namespace DolocTown.UI;

public class ModFunctionButtonGroup : DolocUiObject
{
	private ModFunctionButton[] _subButtons;

	private ModFunctionButton[] subButtons => _subButtons ?? (_subButtons = GetComponentsInChildren<ModFunctionButton>(includeInactive: true));

	public void SetVisibleAndInteractable(bool value, bool setInteractable = true)
	{
		if (!value || setInteractable)
		{
			ModFunctionButton[] array = subButtons;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].interactable = value;
			}
		}
		base.SetVisible(value);
	}
}
