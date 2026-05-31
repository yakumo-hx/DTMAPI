using UnityEngine;

namespace DolocTown;

public class Portal : DolocRecyclableObject
{
	[SerializeField]
	public string portalId;

	[SerializeField]
	protected string srcId;

	[SerializeField]
	protected string roomId;

	[SerializeField]
	protected new Vector2 position;

	private void Start()
	{
		if (TryGetComponent<SpriteRenderer>(out var component))
		{
			component.enabled = false;
		}
	}
}
