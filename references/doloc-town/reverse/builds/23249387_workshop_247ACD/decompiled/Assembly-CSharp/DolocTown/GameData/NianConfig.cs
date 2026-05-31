using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "NianConfig", menuName = "多洛可小镇[地牢]/战斗系统/年兽配置")]
public class NianConfig : ScriptableObject
{
	[SerializeField]
	[Min(1f)]
	public float downTime = 6.5f;

	[SerializeField]
	[Min(1f)]
	public int maxChopCount = 3;

	[SerializeField]
	[Min(1f)]
	public float toolAttack = 25f;

	[SerializeField]
	[Min(0f)]
	public float downDefense = 3f;

	[SerializeField]
	public Vector2 boomForce = new Vector2(9f, 30f);

	[SerializeField]
	public EffectsConfigSO deathEffects;

	[SerializeField]
	[Min(1f)]
	public float firecrackersImmunityTime = 5f;

	[SerializeField]
	public string deathEventName = "slain_nian";

	private IEnumerable<string> CustomEventNames => DolocConfig.Tables.TbCustomEvent.DataList.Select((CustomEventInfo x) => x.Id);
}
