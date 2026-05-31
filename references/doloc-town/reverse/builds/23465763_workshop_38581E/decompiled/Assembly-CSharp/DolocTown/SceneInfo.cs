namespace DolocTown;

public readonly struct SceneInfo
{
	public readonly int id;

	public readonly string name;

	public readonly string shortName;

	public readonly SceneType type;

	public readonly string path;

	public SceneInfo(int sceneBuildIndex, string rawName, string shortName, SceneType sceneType, string scenePath)
	{
		id = sceneBuildIndex;
		name = rawName;
		this.shortName = shortName;
		type = sceneType;
		path = scenePath;
	}
}
