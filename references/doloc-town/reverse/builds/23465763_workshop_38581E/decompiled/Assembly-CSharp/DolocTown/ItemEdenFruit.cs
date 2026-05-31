using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemEdenFruit : Item
{
	public ItemEdenFruit(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemEdenFruit(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		DolocAPI.ShowQuestionBox(DolocUtils.Format(DolocConfig.StaticTexts.ItemConfirmUse, title), Use);
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		if (DolocAPI.userInput.CheckState<TakeBoatState>())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipErrTakeBoat);
		}
		if (!DolocAPI.IsCurrentStateSupportCutscenes)
		{
			return;
		}
		DolocAPI.gameStateManager.agentController.EnterState<AgentStateEat>();
		CostSelf();
		SoundEvents soundEvent = SoundEvents.PLAY_CHARACTER_EAT;
		DolocAPI.Sound.PostSoundEvent(soundEvent);
		DolocAPI.WaitUntil(() => DolocAPI.IsCurrentStateSupportCutscenes, delegate
		{
			DolocAPI.TransitFadeInout(delegate
			{
				ItemFunctionEdenFruit obj = (ItemFunctionEdenFruit)base.proto.Function;
				DolocAPI.UpgradeMaxEnergy(obj.MaxEnergy);
				DolocAPI.UpgradeMaxHealth(obj.MaxHealth);
				DolocAPI.UpgradeMaxSpirit(obj.MaxSpirit);
				DolocAPI.ShowMessageBoxAttention(DolocConfig.StaticTexts.ItemEdenFruitTip);
			}, 3f, 1.5f, 1f);
		});
	}
}
