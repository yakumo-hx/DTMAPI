using UnityEngine;

public class RectSize : MonoBehaviour
{
	public void test()
	{
		Debug.Log($"sizeDelta: {((RectTransform)base.transform).sizeDelta}");
		Debug.Log($"rect.size: {((RectTransform)base.transform).rect.size}");
	}
}
