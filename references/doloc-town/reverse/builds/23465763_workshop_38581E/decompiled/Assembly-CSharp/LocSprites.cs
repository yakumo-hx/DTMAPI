using UnityEngine;

public static class LocSprites
{
	public static Sprite UI_TIP_FILLEDSOCK => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_TIP_FILLEDSOCK);

	public static Sprite UI_TIP_EMPTYSOCK => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_TIP_EMPTYSOCK);

	public static Sprite SPRITE_EMPTY => DolocAPI.GetAsset<Sprite>(DolocGameAssets.SPRITE_EMPTY);

	public static Sprite UI_POINTER_UP => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_POINTER_UP);

	public static Sprite UI_POINTER_DOWN => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_POINTER_DOWN);

	public static Sprite UI_TIP_ANIMAL_FONDLE => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_TIP_ANIMAL_FONDLE);

	public static Sprite UI_TIP_ANIMAL_COLLECT => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_TIP_ANIMAL_COLLECT);

	public static Sprite UI_INFOICON_ATTENTION => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_ATTENTION);

	public static Sprite UI_INFOICON_QUESTION => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_QUESTION);

	public static Sprite UI_INFOICON_ERROR_24PX => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_ERROR_24PX);

	public static Sprite UI_INFOICON_ERROR => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_ERROR);

	public static Sprite UI_INFOICON_COMPLETE_24PX => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_COMPLETE_24PX);

	public static Sprite UI_INFOICON_LOVE_24PX => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_LOVE_24PX);

	public static Sprite UI_INFOICON_HATE_24PX => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_HATE_24PX);

	public static Sprite UI_INFOICON_STAR => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_STAR);

	public static Sprite UI_INFOICON_STAR2 => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_STAR2);

	public static Sprite UI_INFOICON_STAR3 => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_STAR3);

	public static Sprite UI_INFOICON_STAR_24PX => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_STAR_24PX);

	public static Sprite UI_INFOICON_DRONE_28PX => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_DRONE_28PX);

	public static Sprite UI_INFOICON_ACTIVE_28PX => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_INFOICON_ACTIVE_28PX);

	public static Sprite UI_MENUICON_GIFT => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_MENUICON_GIFT);

	public static Sprite UI_MENUICON_PLATFORM => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_MENUICON_PLATFORM);

	public static Sprite UI_MENUICON_BUILDINGS => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_MENUICON_BUILDINGS);

	public static Sprite UI_ICON_DEFAULT24 => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_ICON_DEFAULT24);

	public static Sprite UI_ICON_RECIPE => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_ICON_RECIPE);

	public static Sprite UI_ITEMICON_DEFAULT => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_ITEMICON_DEFAULT);

	public static Sprite UI_BUFFICON_DEFAULT => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_BUFFICON_DEFAULT);

	public static Sprite UI_ICON_GOLD28X => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_ICON_GOLD28X);

	public static Sprite UI_ICON_GOLD16X => DolocAPI.GetAsset<Sprite>(DolocGameAssets.UI_ICON_GOLD16X);

	public static Sprite[] Clone(Sprite sprite, int count)
	{
		Sprite[] array = new Sprite[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = sprite;
		}
		return array;
	}
}
