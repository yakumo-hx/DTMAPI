using UnityEngine;

public static class GeometryUtils
{
	public static Vector2Int[] GetAffectorAroundPositions(int horizontalRange, int verticalRangeTop, int verticalRangeBottom, Vector2Int size)
	{
		int num = horizontalRange * 2 + size.x;
		int num2 = verticalRangeBottom + verticalRangeTop + size.y;
		Vector2Int[] array = new Vector2Int[num * num2];
		int num3 = 0;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				array[num3++] = new Vector2Int(i - horizontalRange, j - verticalRangeBottom);
			}
		}
		return array;
	}

	public static Vector2 CalcRandomIndicatePos(SpriteRenderer sr, Vector2 rangeX, Vector2 rangeY)
	{
		if (sr == null)
		{
			return Vector2.zero;
		}
		Vector2 result = sr.transform.position;
		if (sr.sprite != null)
		{
			Vector2 vector = -sr.sprite.pivot / sr.sprite.rect.size;
			vector.x += Random.Range(rangeX.x, rangeX.y);
			vector.y += Random.Range(rangeY.x, rangeY.y);
			result += vector * 0.125f * sr.sprite.rect.size;
		}
		return result;
	}

	public static Vector2 CalcIndicatePos(SpriteRenderer sr, float rate)
	{
		if (sr == null)
		{
			return Vector2.zero;
		}
		Vector2 result = sr.transform.position;
		if (sr.sprite != null)
		{
			result.y += sr.sprite.rect.height * 0.125f * rate;
		}
		return result;
	}
}
