using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "ExpressDroneConfig", menuName = "多洛可小镇[农场]/其他/ExpressDroneConfig")]
public class ExpressDroneConfig : ScriptableObject
{
	public ExpressDronePerformance performanceTakeOff;

	public ExpressDronePerformance performancePromote;

	public ExpressDronePerformance performanceLanding;
}
