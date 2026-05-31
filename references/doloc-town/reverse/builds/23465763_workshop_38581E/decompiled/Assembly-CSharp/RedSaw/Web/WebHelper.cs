using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine.Device;
using UnityEngine.Networking;

namespace RedSaw.Web;

public static class WebHelper
{
	private static readonly JObject DeviceInfo;

	static WebHelper()
	{
		DeviceInfo = new JObject
		{
			{
				"dvc_code",
				SystemInfo.deviceUniqueIdentifier
			},
			{
				"dvc_type",
				SystemInfo.deviceType.ToString()
			},
			{
				"os",
				SystemInfo.operatingSystem
			}
		};
	}

	public static IEnumerator _Post(string url, string jsonData, string contentType = "application/json", Action<UnityWebRequest> resultHandle = null)
	{
		UnityWebRequest request = new UnityWebRequest(url, "POST")
		{
			uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData)),
			downloadHandler = new DownloadHandlerBuffer()
		};
		request.SetRequestHeader("Content-Type", contentType);
		yield return request.SendWebRequest();
		resultHandle?.Invoke(request);
	}

	public static UnityWebRequest GetPostRequest(string url, string jsonData, string contentType = "application/json")
	{
		UnityWebRequest unityWebRequest = new UnityWebRequest(url, "POST");
		unityWebRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
		unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
		unityWebRequest.SetRequestHeader("Content-Type", contentType);
		return unityWebRequest;
	}

	public static UnityWebRequest GetPostRequestWithDeviceInfo(string url, JObject data, string contentType = "application/json")
	{
		if (data == null)
		{
			return null;
		}
		data.Add("device", DeviceInfo);
		return GetPostRequest(url, data.ToString(), contentType);
	}
}
