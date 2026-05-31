namespace DolocTown.GameData;

public class MissionContentMulti : MissionContent
{
	public enum RequireMode
	{
		All,
		Any
	}

	private readonly RequireMode requireMode;

	public readonly MissionRequire[] missionEvents;

	public MissionContentMulti(MissionRequire[] evts, RequireMode requireMode)
	{
		this.requireMode = requireMode;
		missionEvents = evts;
	}

	public override MissionContentHandle CreateHandle()
	{
		return new MissionContentHandleMulti(this, requireMode);
	}
}
