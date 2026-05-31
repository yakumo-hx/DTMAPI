using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(menuName = "多洛可小镇[地牢]/战斗系统/测试用无人机框架", fileName = "NewDrone")]
public class DroneStructDebugSO : SerializedScriptableObject
{
	[SerializeField]
	private Sprite sceneSprite;

	[SerializeField]
	private DroneStructDebugSlot[] slots;

	public Sprite SceneSprite => sceneSprite;

	public DroneStructDebugSlot[] Slots => slots;
}
