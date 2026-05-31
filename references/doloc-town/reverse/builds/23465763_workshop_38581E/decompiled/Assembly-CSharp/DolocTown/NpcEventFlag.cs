using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[GameEntityManager("/city/npcs/events", DolocGameAssets.GAME_ENTITY_NPC_EVENT_FLAG, CustomManagement = true)]
public class NpcEventFlag : GameEntity
{
	private SpriteRenderer _spriteRenderer;

	protected override void __Init()
	{
		base.__Init();
		_spriteRenderer = GetComponent<SpriteRenderer>();
	}
}
