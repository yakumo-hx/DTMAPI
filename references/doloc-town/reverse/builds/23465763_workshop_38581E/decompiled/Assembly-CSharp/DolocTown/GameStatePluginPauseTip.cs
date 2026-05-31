using DolocTown.UI;

namespace DolocTown;

public class GameStatePluginPauseTip : GameStatePlugin
{
	public override void BeforeSwitch(IDolocGameState lst, IDolocGameState cur)
	{
		GameTimeTip entity = DolocAPI.uiSystem.GetEntity<GameTimeTip>();
		if (!(entity == null))
		{
			if (cur == null)
			{
				entity.SetPauseTipVisible(value: false);
				return;
			}
			bool flag = DolocAPI.IsGameInitialized && DolocAPI.IsDataLoaded && cur.ShowPauseTip;
			entity.SetPauseTipVisible(flag && !DolocAPI.DisableOverlayUi);
		}
	}
}
