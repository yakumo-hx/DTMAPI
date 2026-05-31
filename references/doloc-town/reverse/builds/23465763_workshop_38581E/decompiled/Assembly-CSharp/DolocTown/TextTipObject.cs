using UnityEngine;

namespace DolocTown;

public class TextTipObject : InteractableObject
{
	[SerializeField]
	private TextTipConfig textTipConfig;

	protected override bool notSupportCustomEvent => false;

	private bool showOnInit => false;

	private bool showOnTouch => textTipConfig.isSceneTip;

	private bool showOnInteract => !textTipConfig.isSceneTip;

	private bool hideOnDisTouch => textTipConfig.isSceneTip;

	private void ShowText()
	{
		textTipConfig.ShowText(GetTipPosition());
	}

	private void HideText()
	{
		textTipConfig.HideText();
	}

	protected override void __Init()
	{
		base.__Init();
		if (showOnInit)
		{
			ShowText();
		}
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (showOnTouch)
		{
			ShowText();
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (showOnInteract)
		{
			ShowText();
		}
	}

	protected override void OnDisTouch()
	{
		if (hideOnDisTouch)
		{
			HideText();
		}
		base.OnDisTouch();
	}
}
