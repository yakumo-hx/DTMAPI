using UnityEngine;

namespace DolocTown;

public class VendingMachine : CutsceneObject
{
	[SerializeField]
	private string npcId;

	private Npc _npc;

	private Npc npc
	{
		get
		{
			if (DolocAPI.IsDataLoaded && _npc == null)
			{
				DolocAPI.QueryNpc(npcId, out _npc);
			}
			return _npc;
		}
	}

	private void Update()
	{
		if (DolocAPI.IsDataLoaded)
		{
			bool flag = npc != null && npc.sceneName == DolocAPI.archiveHandle.currentSceneName && Vector2.Distance(npc.positionWS, base.transform.position) <= 1f;
			_collider.enabled = !flag;
		}
	}
}
