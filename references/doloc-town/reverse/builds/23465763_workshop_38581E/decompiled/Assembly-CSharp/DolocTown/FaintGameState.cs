using DolocTown.Config;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class FaintGameState : GameStateNoInput
{
	private readonly FaintReason reason;

	private readonly RSTimer timer;

	private bool isWaiting;

	public FaintGameState(GameStateMachine userInput, FaintReason reason, float waitTime = 3f)
		: base(userInput)
	{
		this.reason = reason;
		isWaiting = true;
		timer = new RSTimer(waitTime);
		gameController.ClearState(DolocAPI.gameStateManager.normalGameState);
	}

	public override void OnFixedUpdate(float dt)
	{
		if (isWaiting && timer.Tick(dt))
		{
			isWaiting = false;
			if (reason == FaintReason.Tired && DolocAPI.archiveHandle.farmData.agentData.agentEquipment.TryResistFaint(shouldRender: true))
			{
				StandUp(wakeUp: false);
			}
			else
			{
				DolocAPI.ppm.FadeIn(2f, OnFadeIn);
			}
		}
	}

	private void OnFadeIn()
	{
		DolocAPI.archiveHandle.PassTimeNoControl(DolocAPI.GlobalParameter.SleepTime);
		DolocAPI.archiveHandle.RenderWeatherAndDayNight();
		if (DolocAPI.CurrentRoom.Type == RoomType.Farm)
		{
			DolocAPI.ppm.FadeOut(2f, OnFadeOut, shouldReset: false, "OnFadeIn");
			return;
		}
		CostMoney();
		DolocAPI.ppm.DisableFadeInOnce = true;
		DolocAPI.DoTransport(DolocAPI.gameManager.gameInitConfig.afterFaintMarkPoint, delegate
		{
			DolocAPI.ppm.DisableFadeOutOnce = true;
			DolocAPI.ppm.FadeOut(2f, OnFadeOut, shouldReset: false, "OnFadeIn");
		});
	}

	private void OnFadeOut()
	{
		DolocAPI.ResetPlayerValues(DolocAPI.GlobalParameter.HealthPercentAfterFaint, DolocAPI.GlobalParameter.EnergyPercentAfterFaint, DolocAPI.GlobalParameter.SpiritPercentAfterFaint);
		StandUp(wakeUp: true);
	}

	private void CostMoney()
	{
		int currentMoney = DolocAPI.archiveHandle.CurrentMoney;
		int num = 200;
		if (currentMoney < 500)
		{
			num = Mathf.FloorToInt((float)currentMoney * 0.4f);
			if (num > 0)
			{
				DolocAPI.archiveHandle.CurrentMoney -= num;
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiLoseMoneyTip.Format(num));
			}
		}
		else
		{
			DolocAPI.archiveHandle.CurrentMoney -= num;
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiLoseMoneyTip.Format(num));
		}
	}

	private void ThrowRandomItems(int count = 2)
	{
		InventorySystem inventorySystem = DolocAPI.archiveHandle.InventorySystem;
		count = Mathf.Min(count, inventorySystem.inventory.filledCount);
		int num = 0;
		string[] array = new string[count];
		for (int i = 0; i < count; i++)
		{
			int num2 = Random.Range(0, inventorySystem.inventory.capacity);
			Item item = inventorySystem[num2];
			if (item != null && item.disposable)
			{
				Item item2 = inventorySystem.Take(num2);
				array[i] = string.Format(DolocConfig.StaticTexts.UiItemSimpleTip, item2.count, item2.title);
				num++;
			}
		}
		if (num > 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(string.Format(DolocConfig.StaticTexts.UiLoseItemTip, string.Join(',', array)));
		}
	}

	private void StandUp(bool wakeUp)
	{
		DolocAPI.agent.Stand(delegate
		{
			DolocAPI.OnWakeUp(saveData: false, wakeUp, wakeUp);
			gameController.ClearState(DolocAPI.gameStateManager.normalGameState);
		});
	}

	public override void OnExit()
	{
		base.OnExit();
		DolocAPI.agent.UnsetFaint();
	}

	public override void OnEnter()
	{
		base.OnEnter();
		DolocAPI.gameStateManager.agentController.GetOffIfRiding();
		DolocAPI.agent.Faint(reason);
	}
}
