using RedSaw;
using UnityEngine;

public class Rule : MonoBehaviour
{
	[SerializeField]
	private Vector2 size;

	[SerializeField]
	private Color c = Color.white;

	public Vector2 pixelSize => new Vector2(size.x * 8f, size.y * 8f);

	public void OnDrawGizmos()
	{
		GizmosHelper.DrawBoxLB(base.transform.position, size, c);
	}
}
