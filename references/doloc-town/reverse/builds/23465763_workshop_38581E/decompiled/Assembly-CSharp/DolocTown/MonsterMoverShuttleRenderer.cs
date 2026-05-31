using UnityEngine;

namespace DolocTown;

public abstract class MonsterMoverShuttleRenderer : MonoBehaviour, IMonsterMoverShuttleRenderer
{
	public abstract void OnShuttleIn();

	public abstract void OnShuttleOut();
}
