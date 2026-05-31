using UnityEngine;

namespace DolocTown;

public class RTWriter : MonoBehaviour
{
	[SerializeField]
	private RenderTexture rt;

	[SerializeField]
	private Vector2Int start;

	[SerializeField]
	private Vector2Int size;

	public void Write()
	{
		Texture2D texture2D = new Texture2D(size.x, size.y);
		for (int i = 0; i < size.x; i++)
		{
			for (int j = 0; j < size.y; j++)
			{
				texture2D.SetPixel(i, j, Color.white);
			}
		}
		texture2D.Apply();
		Graphics.SetRenderTarget(rt);
		GL.Clear(clearDepth: true, clearColor: true, Color.clear);
		Graphics.CopyTexture(texture2D, 0, 0, 0, 0, size.x, size.y, rt, 0, 0, start.x, start.y);
	}
}
