using UnityEngine;

namespace RedSaw;

public static class SpriteUtils
{
	public static Vector2 GetRealSizeBySpriteBorder(this Sprite sprite)
	{
		float x = (float)sprite.texture.width - (sprite.border.x + sprite.border.z);
		float y = (float)sprite.texture.height - (sprite.border.y + sprite.border.w);
		return new Vector2(x, y);
	}
}
