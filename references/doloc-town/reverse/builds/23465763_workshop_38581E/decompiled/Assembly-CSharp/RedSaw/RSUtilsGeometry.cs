using UnityEngine;

namespace RedSaw;

public static class RSUtilsGeometry
{
	public static Vector3[] bezierCurve3(Vector2 start, Vector2 end, Vector2 ctrlpos, Vector2 ctrlpos2, int count)
	{
		Vector3[] array = new Vector3[count];
		Vector2 vector = ctrlpos - start;
		Vector2 vector2 = ctrlpos2 - ctrlpos;
		Vector2 vector3 = end - ctrlpos2;
		for (int i = 0; i < array.Length; i++)
		{
			float num = (float)i / (float)count;
			Vector2 vector4 = start + vector * num;
			Vector2 vector5 = ctrlpos + vector2 * num;
			Vector2 vector6 = ctrlpos2 + vector3 * num;
			Vector2 vector7 = vector4 + (vector5 - vector4) * num;
			Vector2 vector8 = vector5 + (vector6 - vector5) * num;
			array[i] = vector7 + (vector8 - vector7) * num;
		}
		return array;
	}

	public static Vector2 BezierCurve2(float t, Vector2 currentPos, Vector2 controlPos, Vector2 targetPos)
	{
		Vector2 vector = currentPos + (controlPos - currentPos) * t;
		Vector2 vector2 = controlPos + (targetPos - controlPos) * t;
		return vector + (vector2 - vector) * t;
	}

	public static float GetTopPosition(GameObject obj)
	{
		return obj.transform.localScale.y / 2f + obj.transform.position.y;
	}

	public static float GetPositionFromGroundLine(GameObject obj, float groundLine)
	{
		return obj.transform.localScale.y / 2f + groundLine;
	}

	public static void PutOnGroundPivotCenter(GameObject ground, GameObject obj)
	{
		float topPosition = GetTopPosition(ground);
		obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.localScale.y / 2f + topPosition, obj.transform.position.z);
	}

	public static void PutOnGroundPivotBottom(GameObject ground, GameObject obj)
	{
		float topPosition = GetTopPosition(ground);
		obj.transform.position = new Vector3(obj.transform.position.x, topPosition, obj.transform.position.z);
	}

	public static Vector2 GetRectTransformSize(RectTransform box)
	{
		return new Vector2(box.rect.width * box.localScale.x, box.rect.height * box.localScale.y);
	}

	public static void SetRectTransformSize(RectTransform box, Vector2 size)
	{
		Vector2 vector = box.transform.localScale;
		box.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x / vector.x);
		box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y / vector.y);
	}
}
