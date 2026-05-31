using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public class DroneWeaponDebugger : MonoBehaviour
{
	[SerializeField]
	private WeaponDebuggerSO _boundedData;

	public void OnValidate()
	{
		if (_boundedData == null)
		{
			_boundedData = ScriptableObject.CreateInstance<WeaponDebuggerSO>();
		}
	}

	public void RemakeWeapon()
	{
		DroneWeaponInfo proto = _boundedData.RemakeWeapon();
		DolocAPI.CurrentDrone?.DebuggerWeapon(proto);
	}
}
