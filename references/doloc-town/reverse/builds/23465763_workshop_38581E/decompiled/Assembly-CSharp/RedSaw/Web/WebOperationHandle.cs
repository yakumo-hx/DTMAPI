using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using RedSaw.CommandLineInterface;
using UnityEngine;
using UnityEngine.Networking;

namespace RedSaw.Web;

[DisallowMultipleComponent]
public class WebOperationHandle : MonoBehaviour
{
	[SerializeField]
	private int maxRequestCount = 3;

	private int currentRequestCount;

	private readonly Queue<UnityWebRequest> requestQueue = new Queue<UnityWebRequest>();

	public void Test()
	{
		throw new Exception("Test Exception");
	}

	[Command("trigger_example_exception", Desc = "触发案例异常")]
	public static void TriggerExampleException()
	{
		DolocAPI.output("正在触发案例异常");
		UniTask.Delay(500).ContinueWith(delegate
		{
			throw new Exception("this is a example exception");
		}).Forget();
	}

	private static void HandleResult(UnityWebRequest result)
	{
		if (result.result != UnityWebRequest.Result.ConnectionError && result.result != UnityWebRequest.Result.ProtocolError)
		{
			DolocAPI.output("网络请求成功: " + result.url);
			DolocAPI.output("返回信息: " + Regex.Unescape(result.downloadHandler.text));
		}
		else
		{
			DolocAPI.outputWarning("网络请求失败: " + result.url);
			DolocAPI.outputWarning("错误信息: " + result.error);
		}
	}

	public void DoRequest(UnityWebRequest request)
	{
		if (request != null)
		{
			if (currentRequestCount < maxRequestCount)
			{
				StartCoroutine(_HandleRequest(request));
				return;
			}
			DolocAPI.output("当前请求过多，已加入队列");
			requestQueue.Enqueue(request);
		}
	}

	private IEnumerator _HandleRequest(UnityWebRequest request)
	{
		currentRequestCount++;
		yield return request.SendWebRequest();
		HandleResult(request);
		currentRequestCount--;
		if (requestQueue.Count > 0)
		{
			UnityWebRequest request2 = requestQueue.Dequeue();
			StartCoroutine(_HandleRequest(request2));
		}
	}
}
