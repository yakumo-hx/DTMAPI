using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class NpcVision : DolocObject
{
	private Collider2D visionCollider;

	private float visionOffset;

	protected override void __Init()
	{
		base.__Init();
		visionCollider = GetComponent<Collider2D>();
		visionCollider.isTrigger = true;
		visionOffset = visionCollider.offset.x;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!DolocAPI.userInput.CheckState<NormalGameState>())
		{
			return;
		}
		NpcRenderer componentInChildren = other.GetComponentInChildren<NpcRenderer>(includeInactive: true);
		if (componentInChildren != null && componentInChildren.npc != null)
		{
			Vector3 center = componentInChildren.GetComponent<Collider2D>().bounds.center;
			Vector3 center2 = base.transform.parent.GetComponent<Collider2D>().bounds.center;
			if (Mathf.Abs(center.x - center2.x) > visionOffset)
			{
				GetComponentInParent<NpcRenderer>().npc?.OnSeeOtherNpc(componentInChildren.npc);
			}
		}
	}
}
