using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MainMenuPanel : DolocUIPanel
{
	[SerializeField]
	public MenuUI menu;

	[SerializeField]
	private OuterLinkButtonGroup linkButtons;

	public void BuildNavigation()
	{
		Selectable[] array = menu.slots.Where((MenuButton x) => x.isVisible).Select((Func<MenuButton, Selectable>)((MenuButton x) => x.button)).ToArray();
		array.RebuildNavigationHorizontal(array, 1f, 90f, wrapAround: false);
		menu.slots[0].button.SetNavigationOnLeft(linkButtons.slots[0].button);
		menu.slots[^1].button.SetNavigationOnRight(linkButtons.slots[0].button);
		foreach (LinkButton slot in linkButtons.slots)
		{
			slot.button.SetNavigationOnRight(menu.slots[0].button);
			slot.button.SetNavigationOnLeft(menu.slots[^1].button);
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		menu.ResetLayoutSize(menu.slots.Count((MenuButton x) => x.isVisible));
	}
}
