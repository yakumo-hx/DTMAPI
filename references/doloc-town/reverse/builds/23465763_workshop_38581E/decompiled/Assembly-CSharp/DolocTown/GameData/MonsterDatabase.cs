using System.Collections.Generic;

namespace DolocTown.GameData;

public class MonsterDatabase
{
	private readonly Dictionary<string, MonsterProto> protos = new Dictionary<string, MonsterProto>();

	public int TotalCount => protos.Count;

	public IEnumerable<MonsterProto> TotalProtos => protos.Values;

	public bool AddMonster(MonsterProto proto)
	{
		return protos.TryAdd(proto.Name, proto);
	}

	public bool QueryMonster(string name, out MonsterProto proto)
	{
		return protos.TryGetValue(name, out proto);
	}
}
