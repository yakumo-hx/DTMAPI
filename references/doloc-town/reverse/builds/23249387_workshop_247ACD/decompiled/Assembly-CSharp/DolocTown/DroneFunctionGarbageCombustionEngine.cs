using DolocTown.Config.Drone;

namespace DolocTown;

public class DroneFunctionGarbageCombustionEngine : DroneFunction
{
	private readonly DroneFunctionProtoGarbageCombustionEngine _func;

	public DroneFunctionGarbageCombustionEngine(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		_func = (DroneFunctionProtoGarbageCombustionEngine)proto;
	}

	public override bool TryFetchPowerForReloading()
	{
		string[] availableItemNames = _func.AvailableItemNames;
		foreach (string text in availableItemNames)
		{
			if (DolocAPI.CostItem(text, 1, checkBox: true))
			{
				if (DolocAPI.QueryItemProto(text, out var itemInfo))
				{
					DolocAPI.RaiseSpriteFadeUp(DolocAPI.agent.PositionCenter, itemInfo.UiSpriteAsset.Asset);
				}
				return true;
			}
		}
		return false;
	}
}
