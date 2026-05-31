using DolocTown.Config;
using DolocTown.Config.Room;
using UnityEngine;

namespace DolocTown;

public abstract class Station : InteractableObject
{
	[SerializeField]
	private string id;

	private StationInfo _stationProto;

	protected override bool showTip => !(DolocAPI.userInput.CurrentState is TakeBoatState);

	public StationInfo stationProto
	{
		get
		{
			if (_stationProto == null)
			{
				_stationProto = DolocConfig.Tables.TbStation.GetOrDefault(id);
			}
			return _stationProto;
		}
	}

	protected sealed override void OnInteract()
	{
		base.OnInteract();
		if (stationProto == null || !DolocAPI.CheckAvailableInCurrentState(stationProto.Id))
		{
			return;
		}
		if (stationProto.FreeTeleport)
		{
			DolocAPI.EnterUI((TeleportUiState state) => state.HandleStartUpArgs(DoTransport));
		}
		else
		{
			DoTransport(stationProto.MarkPointId);
		}
	}

	protected abstract void DoTransport(string markPointId);
}
