using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[GameEntityManager("/sub_entity/builder_indicator", DolocGameAssets.GAME_ENTITY_BUILDER_INDICATOR)]
public class BuilderIndicator : GameEntity
{
	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private SpriteRenderer rotateSprite;

	public Color indicatorColor
	{
		set
		{
			spriteRenderer.sharedMaterial.SetColor("_BaseColor", value);
		}
	}

	public void SetSprite(Sprite sprite, bool reversible)
	{
		spriteRenderer.sprite = sprite;
		rotateSprite.gameObject.SetActive(reversible);
		if (reversible)
		{
			Vector2Int vector2Int = new Vector2Int(Mathf.RoundToInt(sprite.rect.size.x / 12f), Mathf.RoundToInt(sprite.rect.size.y / 12f));
			Vector2 vector = new Vector2((float)vector2Int.x / 2f, vector2Int.y) * 1.5f;
			rotateSprite.transform.localPosition = vector;
		}
	}
}
