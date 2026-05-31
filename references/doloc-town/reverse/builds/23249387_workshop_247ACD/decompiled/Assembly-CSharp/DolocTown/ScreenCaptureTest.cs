using System.IO;
using UnityEngine;

namespace DolocTown;

public class ScreenCaptureTest : MonoBehaviour
{
	private bool shouldGrab;

	private string savePath;

	public void CaptureScreen(string path)
	{
		DolocAPI.CaptureScreen(path);
	}

	public void GrabScreen(string path)
	{
		RenderTexture renderTexture = new RenderTexture(1920, 1080, 24);
		Camera.main.targetTexture = renderTexture;
		Texture2D texture2D = new Texture2D(1920, 1080, TextureFormat.RGB24, mipChain: false);
		Camera.main.Render();
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
		Camera.main.targetTexture = null;
		RenderTexture.active = null;
		Object.Destroy(renderTexture);
		byte[] bytes = texture2D.EncodeToPNG();
		File.WriteAllBytes(path, bytes);
		Debug.Log("Screen captured at: " + path);
	}
}
