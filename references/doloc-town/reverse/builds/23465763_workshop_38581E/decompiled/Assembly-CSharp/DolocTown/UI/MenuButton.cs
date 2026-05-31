using UnityEngine;

namespace DolocTown.UI;

public class MenuButton : DolocNavigationButton
{
	[SerializeField]
	private AutoSizeText txtTitle;

	[SerializeField]
	private CanvasGroup txtCanvasGroup;

	public string title
	{
		set
		{
			txtTitle.text = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		txtTitle.Init();
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		txtTitle.backgroundColor = DolocUiColor.BACKCOLOR_LIGHT;
		txtTitle.textColor = (base.grayed ? DolocUiColor.SLIENTCOLOR_RED : DolocUiColor.BACKCOLOR_LEVEL3);
	}

	protected override void OnDeselect()
	{
		base.OnDeselect();
		txtTitle.backgroundColor = DolocColor.empty;
		txtTitle.textColor = (base.grayed ? DolocUiColor.SLIENTCOLOR_RED : DolocUiColor.BACKCOLOR_LIGHT);
	}

	public void IgnoreParentCanvasGroups(bool value)
	{
		txtCanvasGroup.ignoreParentGroups = value;
	}
}
