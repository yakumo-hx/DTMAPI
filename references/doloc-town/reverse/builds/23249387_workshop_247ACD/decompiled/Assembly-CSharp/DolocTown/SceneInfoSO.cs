using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct SceneInfoSO
{
	[SerializeField]
	public int id;

	[SerializeField]
	public string name;

	[SerializeField]
	public string shortName;

	[SerializeField]
	public SceneType sceneType;

	[SerializeField]
	public string scenePath;

	public readonly SceneInfo Proto => new SceneInfo(id, name, shortName, sceneType, scenePath);

	public SceneInfoSO(int sceneBuildIndex, string rawName, string shortName, SceneType sceneType, string scenePath)
	{
		id = sceneBuildIndex;
		name = rawName;
		this.shortName = shortName;
		this.sceneType = sceneType;
		this.scenePath = scenePath;
	}
}
