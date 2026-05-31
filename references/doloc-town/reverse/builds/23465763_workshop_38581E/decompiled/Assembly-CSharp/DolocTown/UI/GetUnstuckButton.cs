using UnityEngine;

namespace DolocTown.UI;

public class GetUnstuckButton : LinkButton
{
	public new bool visible;

	protected override void __Init()
	{
		base.__Init();
		base.gameObject.SetActive(visible);
	}

	public override void OpenOuterLink()
	{
		DolocAPI.userInput.PopState();
		DolocAPI.DelayFrame(delegate
		{
			if (DolocAPI.GetStuckOutPosition(DolocAPI.AgentPosition + new Vector3(0f, 0.5f), out var positionWS))
			{
				DolocAPI.AgentPosition = positionWS;
			}
		});
	}
}
