using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemRandomPackage : Item
{
	public ItemRandomPackage(ItemInfo proto, int count = 1)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	public ItemRandomPackage(string itemName, int count)
		: base(itemName, count)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Use();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		ItemFunctionBase function2 = base.proto.Function;
		ItemFunctionRandomPackage function = function2 as ItemFunctionRandomPackage;
		if (function == null)
		{
			return;
		}
		DolocAPI.ShowQuestionBox(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationUseSomething, title), delegate
		{
			if (function.DropSpawnEntry.SpawnLut_Ref == null)
			{
				Debug.LogError("未知的掉落库:" + function.DropSpawnEntry.SpawnLut);
			}
			else
			{
				CostSelf(showFadeUpIcon: false);
				Vector3 agentPosition = DolocAPI.AgentPosition;
				DolocAPI.RaiseInstantPSEffects(agentPosition, InstantParticleEffectsType.SPARKS);
				DolocAPI.RaiseInstantAnimEffects(agentPosition, InstAnimEffectType.HIT_SPARK);
				DolocAPI.GenerateDropItems(DolocAPI.CurrentRoom, function.DropSpawnEntry, agentPosition);
			}
		});
	}
}
