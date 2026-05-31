using UnityEngine;

namespace DolocTown;

public class CloudShadowDebugger : MonoBehaviour
{
	private void DisableAll()
	{
		DolocAPI.EnvCovariantController.CloudShadowController.Disable();
	}

	private void EnableAll()
	{
		DolocAPI.EnvCovariantController.CloudShadowController.Enable();
	}

	private void GenerateNew()
	{
		DolocAPI.EnvCovariantController.CloudShadowController.SpawnCloud();
	}
}
