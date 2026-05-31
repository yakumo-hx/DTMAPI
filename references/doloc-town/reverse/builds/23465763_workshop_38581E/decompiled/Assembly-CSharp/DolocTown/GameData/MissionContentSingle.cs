namespace DolocTown.GameData;

public class MissionContentSingle : MissionContent
{
	public readonly MissionRequire require;

	public MissionContentSingle(MissionRequire require)
	{
		this.require = require;
	}

	public override MissionContentHandle CreateHandle()
	{
		return new MissionContentHandleSingle(this);
	}
}
