using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SecondOrderSystem))]
public class SecondOrderSystemDebugger : MonoBehaviour
{
	[SerializeField]
	private Transform follow;

	private SecondOrderSystem system;

	private void Start()
	{
		system = GetComponent<SecondOrderSystem>();
		system.SetTarget(follow, Vector2.zero);
	}

	private void FixedUpdate()
	{
		if (DolocAPI.IsDataLoaded)
		{
			system.OnFixedUpdate(Time.fixedDeltaTime);
		}
	}
}
