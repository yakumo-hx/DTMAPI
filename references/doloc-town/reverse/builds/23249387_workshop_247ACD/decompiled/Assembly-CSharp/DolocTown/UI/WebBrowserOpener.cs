using UnityEngine;

namespace DolocTown.UI;

public class WebBrowserOpener : MonoBehaviour
{
	[SerializeField]
	private string url;

	public void SetUrl(string url)
	{
		this.url = url;
	}

	public void OpenWebBrowser()
	{
		try
		{
			Application.OpenURL(url);
		}
		catch
		{
			Debug.LogError("无法打开浏览器");
		}
	}
}
