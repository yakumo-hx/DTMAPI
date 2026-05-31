using UnityEngine;

namespace DolocTown;

[GameEntityManager("/city/npc_render_objects/", DolocGameAssets.GAME_ENTITY_NPC_OBJECT_LIGHTMAN_FISHING_ROPE)]
public class LightManFishingRope : NpcRenderObject
{
	[SerializeField]
	public RopeRenderer _ropeRenderer;

	protected override void __Init()
	{
		base.__Init();
		_ropeRenderer.Init();
		SetVisible(value: false);
	}
}
