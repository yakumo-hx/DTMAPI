using UnityEngine;

namespace DolocTown;

public interface IDismantleable
{
	void OnFell(Vector2 hitPosition);

	void OnRecover();
}
