using RedSaw.CommandLineInterface;

namespace DolocTown;

public class DungeonResourceHostDebugger
{
	[CommandProperty("DungeonResourceDebugger")]
	public static DungeonResourceHostDebugger instance = new DungeonResourceHostDebugger();

	private bool TryGetHost<T>(out T data) where T : class
	{
		if (DolocAPI.archiveHandle.currentRoom is T val)
		{
			data = val;
			return true;
		}
		data = null;
		return false;
	}
}
