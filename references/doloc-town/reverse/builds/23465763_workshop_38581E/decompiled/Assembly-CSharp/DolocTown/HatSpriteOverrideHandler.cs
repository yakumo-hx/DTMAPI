using UnityEngine;

namespace DolocTown;

[DefaultExecutionOrder(0)]
public class HatSpriteOverrideHandler : SpriteOverrideHandler
{
	protected override bool useRuntimeOverride => true;

	protected override bool TryGetRuntimeOverrideSprite(string oldSpriteName, out Sprite overrideSprite)
	{
		overrideSprite = null;
		if (!TryGetOverrideSpriteName(oldSpriteName, out var overrideSpriteName))
		{
			return false;
		}
		overrideSprite = DolocAPI.GetAsset<Sprite>(overrideSpriteName, useLog: false);
		return overrideSprite != null;
	}

	protected override bool TryGetModOverrideSprite(string oldSpriteName, out Sprite overrideSprite)
	{
		overrideSprite = null;
		if (!TryGetOverrideSpriteName(oldSpriteName, out var overrideSpriteName))
		{
			return false;
		}
		return DolocAPI.modManager.LoadSpriteFromFile(overrideSpriteName, out overrideSprite);
	}

	private bool TryGetOverrideSpriteName(string oldSpriteName, out string overrideSpriteName)
	{
		overrideSpriteName = string.Empty;
		Item hatItem = DolocAPI.archiveHandle.farmData.agentData.agentEquipment.hatItem;
		if (hatItem == null)
		{
			return false;
		}
		string newValue = hatItem.name;
		string oldValue = animator.runtimeAnimatorController.name.Replace("game_anim_hat_", "");
		overrideSpriteName = oldSpriteName.Replace(oldValue, newValue);
		return true;
	}
}
