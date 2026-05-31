using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Player;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AgentEquipmentFunctionExtraResource : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoExtraResource protoExtraResource;

	private readonly HashSet<string> resourceIds = new HashSet<string>();

	public AgentEquipmentFunctionExtraResource(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		protoExtraResource = (AgentEquipmentFuncProtoExtraResource)proto;
		string[] array = protoExtraResource.ResourceIds;
		foreach (string text in array)
		{
			resourceIds.Add(text);
		}
	}

	public override void OnReceiveMessage(GameMessage message)
	{
		if (message.Type != GameEventType.FELL_DUNGEON_RESOURCE || !(message.Args is GameEventArgsString gameEventArgsString) || !resourceIds.Contains(gameEventArgsString.value) || !RandomUtils.Dice(protoExtraResource.Probability))
		{
			return;
		}
		ItemSpawnInfo lut = DolocConfig.Tables.TbItemSpawn.GetOrDefault(protoExtraResource.ItemLut);
		if (lut == null)
		{
			Debug.LogWarning("装备技能：未找到名称为\"" + gameEventArgsString.value + "\"的查找表");
			return;
		}
		int totalCount = protoExtraResource.Range.DiceCount(0);
		if (totalCount == 0)
		{
			return;
		}
		DolocAPI.Delay(0.15f, delegate
		{
			DolocAPI.RaiseInstantPSEffects(DolocAPI.AgentPosition, InstantParticleEffectsType.SPARKS);
			CountItem[] array = lut.SpawnItems(totalCount);
			for (int i = 0; i < array.Length; i++)
			{
				CountItem countItem = array[i];
				DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, countItem.itemName, DolocAPI.Agent.position2d, countItem.itemCount);
			}
		});
	}
}
