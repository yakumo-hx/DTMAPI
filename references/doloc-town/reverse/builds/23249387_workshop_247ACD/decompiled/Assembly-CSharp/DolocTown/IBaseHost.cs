namespace DolocTown;

public interface IBaseHost
{
	Room CurrentRoom { get; }

	Terrain DM_terrain => CurrentRoom.DM_terrain;
}
