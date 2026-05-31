using UnityEngine;

namespace DolocTown;

[DefaultExecutionOrder(0)]
public class PlayerSpriteOverrideHandler : SpriteOverrideHandler
{
	private PlayerSplitRenderer splitRenderer;

	protected override bool useRuntimeOverride => true;

	private void Start()
	{
		splitRenderer = GetComponentInChildren<PlayerSplitRenderer>(includeInactive: true);
	}

	protected override bool TryGetRuntimeOverrideSprite(string oldSpriteName, out Sprite overrideSprite)
	{
		overrideSprite = null;
		if (DolocAPI.TryGetAsset<Sprite>(oldSpriteName.Replace("player", "player_hair"), out var asset))
		{
			splitRenderer.HairRenderer.sprite = asset;
		}
		if (DolocAPI.TryGetAsset<Sprite>(oldSpriteName.Replace("player", "player_body"), out var asset2))
		{
			splitRenderer.BodyRenderer.sprite = asset2;
		}
		return DolocAPI.TryGetAsset<Sprite>(oldSpriteName, out overrideSprite);
	}

	protected override bool TryGetModOverrideSprite(string oldSpriteName, out Sprite overrideSprite)
	{
		overrideSprite = null;
		if (DolocAPI.modManager.LoadSpriteFromFile(oldSpriteName.Replace("player", "player_hair"), out var asset))
		{
			splitRenderer.HairRenderer.sprite = asset;
		}
		if (DolocAPI.modManager.LoadSpriteFromFile(oldSpriteName.Replace("player", "player_body"), out var asset2))
		{
			splitRenderer.BodyRenderer.sprite = asset2;
		}
		return DolocAPI.modManager.LoadSpriteFromFile(oldSpriteName, out overrideSprite);
	}
}
