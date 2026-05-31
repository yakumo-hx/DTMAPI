using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "Bullet Mover", menuName = "多洛可小镇[地牢]/战斗系统/子弹移动逻辑")]
public class BulletMoverConfig : SerializedScriptableObject
{
	[SerializeField]
	private BulletMoverSO mover = new BulletMoverSOLinear();

	public BulletMoverProto CreateProto()
	{
		return mover.CreateProto(base.name);
	}
}
