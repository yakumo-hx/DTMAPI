namespace DolocTown;

public interface IMonsterMoverShuttleRenderer
{
	public class FallbackRenderer : IMonsterMoverShuttleRenderer
	{
		public void OnShuttleIn()
		{
		}

		public void OnShuttleOut()
		{
		}
	}

	static IMonsterMoverShuttleRenderer Fallback;

	void OnShuttleIn();

	void OnShuttleOut();

	static IMonsterMoverShuttleRenderer()
	{
		Fallback = new FallbackRenderer();
	}
}
