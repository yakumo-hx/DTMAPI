using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class Wallpaper
{
	[JsonProperty]
	public string wallpaperName;

	[JsonConstructor]
	protected Wallpaper(string wallpaperName)
	{
		this.wallpaperName = wallpaperName;
	}
}
