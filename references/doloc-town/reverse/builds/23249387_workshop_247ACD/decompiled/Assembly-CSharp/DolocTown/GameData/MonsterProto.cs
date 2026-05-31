using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Monster;
using DolocTown.MonsterAttackBehaviours;

namespace DolocTown.GameData;

public readonly struct MonsterProto : ISpawnedItem
{
	public readonly string Name;

	public readonly MonsterMoverProto MoverProto;

	public readonly IMonsterAttackBehaviour[] AttackBehaviourProtos;

	public int Health => BaseInfo.Health;

	public int Defense => BaseInfo.Defense;

	public float HurtDuration => BaseInfo.HurtDuration;

	public bool IsAir => BaseInfo.IsAir;

	public int ExpValue => BaseInfo.ExpValue;

	public string DeadSound => BaseInfo.DeadSound;

	public string HurtSound => BaseInfo.HurtSound;

	public string HummingSound => BaseInfo.HummingSound;

	private MonsterInfo BaseInfo => DolocConfig.Tables.TbMonster.GetOrDefault(Name);

	public IEffects DeadEffects => DolocAPI.GetAsset<EffectsConfigSO>(BaseInfo.DeadEffects);

	public string SpawnId => Name;

	public int Volume => BaseInfo.Size;

	public ItemSpawnEntry DropSpawnEntry => BaseInfo.DropSpawnEntry;

	public MonsterProto(string name, MonsterMoverProto moverProto, IMonsterAttackBehaviour[] attackBehaviourProtos)
	{
		Name = name;
		MoverProto = moverProto;
		AttackBehaviourProtos = attackBehaviourProtos;
	}
}
