using System;
using System.Collections.Generic;
using System.IO;
using DolocTown;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ImageReader : MonoBehaviour
{
	private const string configPath = "./redsaw/config.json";

	private Dictionary<string, Transform> containers;

	private Queue<GameObject> recycleList;

	private List<GameObject> allDrafts;

	[SerializeField]
	private GameObject pfb;

	public void init(string[] sceneNames)
	{
		containers = new Dictionary<string, Transform>();
		allDrafts = new List<GameObject>();
		recycleList = new Queue<GameObject>();
		foreach (string key in sceneNames)
		{
			GameObject gameObject = new GameObject(key);
			gameObject.transform.SetParent(base.transform);
			containers.Add(key, gameObject.transform);
			gameObject.SetActive(value: false);
		}
	}

	public void setEnabled(string name, bool value)
	{
		if (containers.ContainsKey(name))
		{
			containers[name].gameObject.SetActive(value);
		}
	}

	public void reload()
	{
		foreach (GameObject allDraft in allDrafts)
		{
			recycleList.Enqueue(allDraft);
		}
		allDrafts.Clear();
		if (File.Exists("./redsaw/config.json"))
		{
			string value = File.ReadAllText("./redsaw/config.json");
			try
			{
				foreach (JToken item in JsonConvert.DeserializeObject<JArray>(value))
				{
					handleToken(item);
				}
				return;
			}
			catch (Exception)
			{
				DolocAPI.outputError("配置文件格式错误");
				return;
			}
		}
		DolocAPI.outputError("建筑草稿创建错误:配置文件丢失..");
	}

	public bool handleToken(JToken token)
	{
		try
		{
			string text = token.Value<string>("属于");
			string filepath = token.Value<string>("路径");
			Vector2 pos = JsonUtils.ToVector2(token.Value<JArray>("位置"));
			if (containers.ContainsKey(text))
			{
				return createBuilding(filepath, pos, containers[text]);
			}
			DolocAPI.outputError("场景名" + text + "不存在");
			return false;
		}
		catch (Exception)
		{
			DolocAPI.outputError("解析时遇到异常");
			return false;
		}
	}

	public bool createBuilding(string filepath, Vector2 pos, Transform parent)
	{
		Texture2D texture2D = loadPng(filepath);
		if (texture2D != null)
		{
			texture2D.filterMode = FilterMode.Point;
			Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.zero, 8f);
			GameObject gameObject = createGameObject(parent);
			gameObject.transform.position = new Vector3(pos.x, pos.y, 0f);
			gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
			allDrafts.Add(gameObject);
			return true;
		}
		return false;
	}

	public GameObject createGameObject(Transform transform)
	{
		if (recycleList.Count > 0)
		{
			GameObject obj = recycleList.Dequeue();
			obj.transform.SetParent(transform);
			return obj;
		}
		return UnityEngine.Object.Instantiate(pfb, transform);
	}

	public Texture2D loadPng(string path)
	{
		Texture2D texture2D = null;
		if (File.Exists(path))
		{
			byte[] data = File.ReadAllBytes(path);
			texture2D = new Texture2D(2, 2);
			texture2D.LoadImage(data);
		}
		return texture2D;
	}
}
