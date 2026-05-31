using System;
using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("根据名称生成怪物", 0)]
[Description("根据指定的怪物名称列表在当前房间生成对应的怪物")]
[Category("多洛可小镇/地牢")]
public class DialogueTask_SpawnMonsterByName : DialogueTask
{
	[Serializable]
	public struct MonsterInfo
	{
		[SerializeField]
		public string name;

		[SerializeField]
		public int count;

		[SerializeField]
		public bool customPosition;

		[SerializeField]
		public Vector2 position;

		public MonsterInfo(string name, int count, bool customPosition = false, Vector2 position = default(Vector2))
		{
			this.name = name;
			this.count = count;
			this.customPosition = customPosition;
			this.position = position;
		}
	}

	[SerializeField]
	private bool customRoom;

	[SerializeField]
	private string targetRoom;

	[SerializeField]
	private List<MonsterInfo> monstersToSpawn;

	public override string taskTitle
	{
		get
		{
			if (customRoom)
			{
				return "在房间\"" + targetRoom + "\"生成一组怪物";
			}
			return "生成一组怪物";
		}
	}

	private IMonsterHost GetMonsterHost()
	{
		if (customRoom)
		{
			return DolocAPI.GetRoom(targetRoom);
		}
		return DolocAPI.CurrentRoom;
	}

	public override void DoAction(Graph graph)
	{
		IMonsterHost monsterHost = GetMonsterHost();
		if (monsterHost == null)
		{
			return;
		}
		bool shouldRender = DolocAPI.CurrentRoom == monsterHost;
		Vector2 roomPosition = DolocAPI.CurrentRoom.Geometry.roomPosition;
		foreach (MonsterInfo item in monstersToSpawn)
		{
			if (!DolocAPI.assets.monsters.QueryMonster(item.name, out var proto))
			{
				Debug.LogWarning("DialogueTask_SpawnMonsterByName: 未找到怪物 " + item.name);
				continue;
			}
			for (int i = 0; i < item.count; i++)
			{
				if (item.customPosition)
				{
					monsterHost.GenerateMonster(proto, item.position + roomPosition, shouldRender);
				}
				else
				{
					monsterHost.GenerateMonster(proto, shouldRender);
				}
			}
		}
	}
}
