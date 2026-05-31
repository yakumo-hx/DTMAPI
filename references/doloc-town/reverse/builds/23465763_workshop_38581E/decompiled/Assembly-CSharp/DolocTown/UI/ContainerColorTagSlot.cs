using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ContainerColorTagSlot : DolocNavigationButton
{
	[SerializeField]
	private Image mark;

	[SerializeField]
	private Image border;

	protected override void __Init()
	{
		base.__Init();
		mark.gameObject.SetActive(value: false);
		border.gameObject.SetActive(value: false);
	}

	protected override void OnHighLighted(bool value)
	{
		base.OnHighLighted(value);
		mark.gameObject.SetActive(value);
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		border.gameObject.SetActive(value: true);
		this.HideItemBorder();
	}

	protected override void OnDeselect()
	{
		base.OnDeselect();
		border.gameObject.SetActive(value: false);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		mark.gameObject.SetActive(value: false);
		border.gameObject.SetActive(value: false);
	}
}
