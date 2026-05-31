using UnityEngine;

namespace DolocTown;

public struct ActionIconGroup
{
	private string displayName;

	private SpriteAsset largeIconAsset;

	private SpriteAsset smallIconAsset;

	public string largeIconUrl => largeIconAsset?.AssetUrl ?? "";

	public Sprite largeIcon => largeIconAsset?.Asset;

	public string smallIconUrl => smallIconAsset?.AssetUrl ?? "";

	public Sprite smallIcon => smallIconAsset?.Asset;

	public bool Valid => !displayName.IsNullOrEmpty();

	public ActionIconGroup(string displayName, SpriteAsset largeIconAsset, SpriteAsset smallIconAsset)
	{
		this.displayName = displayName;
		this.largeIconAsset = largeIconAsset;
		this.smallIconAsset = smallIconAsset;
	}
}
