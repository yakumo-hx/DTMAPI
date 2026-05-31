using UnityEngine;

public static class GameResolutionUtils
{
	public static Vector2Int Size(this GameResolution resolution)
	{
		return resolution switch
		{
			GameResolution._1920x1080 => new Vector2Int(1920, 1080), 
			GameResolution._1600x900 => new Vector2Int(1600, 900), 
			GameResolution._1366x768 => new Vector2Int(1366, 768), 
			GameResolution._1280x720 => new Vector2Int(1280, 720), 
			GameResolution._1024x768 => new Vector2Int(1024, 768), 
			GameResolution._800x600 => new Vector2Int(800, 600), 
			GameResolution._640x480 => new Vector2Int(640, 480), 
			GameResolution._320x240 => new Vector2Int(320, 240), 
			_ => new Vector2Int(1920, 1080), 
		};
	}
}
