using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemAnimation : Item
{
	protected ItemFunctionAnimationBase func => base.proto.Function as ItemFunctionAnimationBase;

	public ItemAnimation(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemAnimation(string itemName, int itemCount)
		: base(itemName, itemCount)
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
		if (DolocAPI.userInput.CheckState<TakeBoatState>())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipErrTakeBoat);
		}
		if (!DolocAPI.IsCurrentStateSupportCutscenes)
		{
			return;
		}
		if (DolocAPI.IsAgentRiding && !availableIfRiding)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrCannotUseIfRiding);
			return;
		}
		if (CheckCondition())
		{
			if (func.ConfirmMessage.IsNullOrEmpty())
			{
				UseItemInternal();
			}
			else
			{
				DolocAPI.DelayFrame(delegate
				{
					DolocAPI.ShowQuestionBox(func.ConfirmMessage, UseItemInternal);
				});
			}
		}
		DolocAPI.Broadcast(OperationEventType.USE_ITEM);
	}

	private void UseItemInternal()
	{
		if (DolocAPI.StartDialogueNode(func.DialogueNode))
		{
			CostSelf(showFadeUpIcon: false);
		}
	}

	protected virtual bool CheckCondition()
	{
		return true;
	}
}
