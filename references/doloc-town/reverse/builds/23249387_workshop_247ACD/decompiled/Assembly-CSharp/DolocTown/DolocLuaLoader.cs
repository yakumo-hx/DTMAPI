using System.IO;

namespace DolocTown;

public static class DolocLuaLoader
{
	public static byte[] LoadLua(ref string path)
	{
		string path2 = "Assets/Scripts/Lua/" + path + ".lua";
		if (!File.Exists(path2))
		{
			return null;
		}
		return File.ReadAllBytes(path2);
	}

	public static string ReadLua(string path)
	{
		string path2 = "Assets/Scripts/Lua/" + path + ".lua";
		if (!File.Exists(path2))
		{
			return null;
		}
		return File.ReadAllText(path2);
	}
}
