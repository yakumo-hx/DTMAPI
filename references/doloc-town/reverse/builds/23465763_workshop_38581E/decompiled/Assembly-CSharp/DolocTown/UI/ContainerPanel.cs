using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ContainerPanel : AutoSizeUIPanel
{
	[SerializeField]
	public BackpackBottomPanel backpackPanel;

	[FormerlySerializedAs("containerPanel")]
	[SerializeField]
	public ContainerWidget containerWidget;

	[SerializeField]
	public GameObject raycastMask;

	protected override void __Init()
	{
		base.__Init();
		SetRaycastMaskEnable(value: false);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		DolocAPI.DelayFrame(delegate
		{
			backpackPanel.Select(DolocAPI.SelectedItemIndex);
		});
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		RebuildNavigation();
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		SetRaycastMaskEnable(value: false);
	}

	public void RebuildNavigation()
	{
		ContainerColorTagUI containerColorTagUI = containerWidget.containerColorTagUI;
		Selectable[] array = backpackPanel.allSelectablesArray.Concat(containerWidget.allSelectablesArray).ToArray();
		if (containerColorTagUI.isRender)
		{
			array = array.Concat(containerColorTagUI.allSelectablesArray).ToArray();
			containerColorTagUI.BuildNavigation();
		}
		backpackPanel.RebuildNavigationVertical(array);
		containerWidget.BuildNavigation();
		containerWidget.RebuildNavigationVertical(array);
		if (containerColorTagUI.isRender)
		{
			containerColorTagUI.RebuildNavigationVertical(array);
		}
	}

	public void SetRaycastMaskEnable(bool value)
	{
		raycastMask.gameObject.SetActive(value);
	}
}
