using System;
using DolocTown.Config;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class TouchIndicator : InteractableObject
{
	[SerializeField]
	private GameObject indicator;

	[SerializeField]
	public bool showRemoteDoorInformation;

	public Action onTouchCallback;

	public Action onDisTouchCallback;

	protected override void __Init()
	{
		base.__Init();
		indicator.gameObject.SetActive(value: false);
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		indicator.SetActive(value: true);
		if (showRemoteDoorInformation)
		{
			BuildingLinkGate componentInParent = base.transform.parent.GetComponentInParent<BuildingLinkGate>();
			if (componentInParent == null || componentInParent.LinkInfo == null)
			{
				Debug.LogError("TouchIndicator未找到父级BuildingLinkGate组件");
				return;
			}
			Vector2 vector = new Vector2(DolocAPI.AgentPosition.x, base.transform.position.y + 4.5f);
			string prompt = string.Format(DolocConfig.StaticTexts.UiOperationEnterFormat, componentInParent.LinkInfo.targetBuilding.Title);
			this.ShowSceneOperationTip(vector, prompt);
		}
		onTouchCallback?.Invoke();
	}

	protected override void OnDisTouch()
	{
		indicator.SetActive(value: false);
		this.HideSceneOperationTip();
		onDisTouchCallback?.Invoke();
		base.OnDisTouch();
	}
}
