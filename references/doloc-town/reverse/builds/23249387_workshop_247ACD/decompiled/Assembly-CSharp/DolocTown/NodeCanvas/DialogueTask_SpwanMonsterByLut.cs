using DolocTown.Config;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("根据查找表生成怪物", 0)]
[Description("根据特定的查找表在当前房间生成怪物")]
[Category("多洛可小镇/地牢")]
public class DialogueTask_SpwanMonsterByLut : DialogueTask
{
	[SerializeField]
	private string monsterLut;

	[SerializeField]
	private Vector2Int countRange;

	[SerializeField]
	private bool useFixedCount;

	[SerializeField]
	private int fixedCount;

	public override string taskTitle => "基于查找表\"" + monsterLut + "\"生成怪物";

	public override void DoAction(Graph graph)
	{
		IMonsterHost currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom == null)
		{
			Debug.LogError("DialogueTask_SpwanMonster: 当前房间为空");
			return;
		}
		if (!DolocConfig.Tables.TbMonsterSpawn.DataMap.TryGetValue(monsterLut, out var value))
		{
			Debug.LogError("DialogueTask_SpwanMonster: 未找到怪物查找表 " + monsterLut);
			return;
		}
		int totalCount = (useFixedCount ? fixedCount : Random.Range(countRange.x, countRange.y + 1));
		currentRoom.GenerateMonstersData(value, totalCount);
	}
}
