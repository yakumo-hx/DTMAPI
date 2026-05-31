using UnityEngine;

namespace DolocTown;

public interface ITouchCheckStrategy
{
	bool Check(GameObject other);
}
