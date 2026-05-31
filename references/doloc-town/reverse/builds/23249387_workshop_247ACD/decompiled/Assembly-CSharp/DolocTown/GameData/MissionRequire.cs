using Newtonsoft.Json;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public abstract class MissionRequire
{
	public abstract MissionRequireHandle CreateHandle();
}
