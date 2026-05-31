using UnityEngine;

namespace DolocTown;

[GameEntityManager("farm/building/windows", DolocGameAssets.GAME_ENTITY_BUILDING_WINDOW)]
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingWindow : GameEntity
{
	public void Render(Sprite maskSprite, Vector2 position)
	{
		this.position = new Vector3(position.x, position.y, 200f);
		GetComponent<SpriteRenderer>().sprite = maskSprite;
	}
}
