using System.Collections.Generic;
using DolocTown.GameData;

namespace DolocTown;

public class MonsterManager
{
	private readonly List<Monster> monsters = new List<Monster>();

	public IEnumerable<Monster> AllMonsters => monsters;

	public bool HasNoMonster => monsters.Count == 0;

	public bool HasMonster => monsters.Count > 0;

	public int MonsterCount => monsters.Count;

	public bool IsRunning { get; private set; }

	public Monster CreateMonster(MonsterProto proto)
	{
		Monster monster = new Monster(proto);
		monsters.Add(monster);
		return monster;
	}

	public void SetIsRunning(bool value)
	{
		IsRunning = value;
	}

	public void Clear()
	{
		monsters.Clear();
	}

	public bool RemoveMonster(Monster monster)
	{
		return monsters.Remove(monster);
	}
}
