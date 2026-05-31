using UnityEngine;

namespace DolocTown;

public interface IHasSubscript
{
	bool HasSubscript => SubscriptSprite != null;

	Sprite SubscriptSprite { get; }
}
