using UnityEngine;

namespace DolocTown;

public class RoomScanner : IScanner
{
	private readonly InteractableManagerEx _manager;

	public DungeonResource currentResource;

	private Room currentRoom;

	private bool shouldNotCheck = true;

	private Equipment _currentEquipment;

	public Vector2 lastWorldPosition { get; private set; }

	public Vector2Int lastCellPosition { get; private set; }

	public Equipment CurrentEquipment
	{
		get
		{
			return _currentEquipment;
		}
		private set
		{
			if (value == null)
			{
				if (_currentEquipment != null)
				{
					_currentEquipment.DisableOutline();
					_manager.DisTouch(_currentEquipment.Interactable);
				}
				_currentEquipment = null;
			}
			else if (_currentEquipment != value)
			{
				if (_currentEquipment != null)
				{
					_manager.DisTouch(_currentEquipment.Interactable);
				}
				_currentEquipment = value;
				_manager.Touch(_currentEquipment.Interactable);
			}
		}
	}

	public RoomScanner(InteractableManagerEx manager)
	{
		_manager = manager;
	}

	public void ClearBuffer()
	{
		lastWorldPosition = Vector2.zero;
		lastCellPosition = Vector2Int.zero;
		if (CurrentEquipment != null)
		{
			CurrentEquipment = null;
		}
		currentResource = null;
	}

	public void OnRoomChanged(Room room)
	{
		currentRoom = room;
		shouldNotCheck = false;
	}

	public void OnWorldPosChanged(Vector2 positionWS)
	{
		if (!shouldNotCheck)
		{
			lastWorldPosition = positionWS;
		}
	}

	public void OnPosChanged(Vector2Int pos)
	{
		if (currentRoom != null)
		{
			lastCellPosition = pos;
			Equipment currentEquipment = CurrentEquipment;
			CurrentEquipment = FindEquipment(pos);
			if (currentEquipment != CurrentEquipment)
			{
				currentEquipment?.DisableOutline();
				CurrentEquipment?.ShowOutline();
			}
		}
	}

	private Equipment FindEquipment(Vector2Int pos)
	{
		Equipment equipment = ((IEquipmentHost)currentRoom).GetEquipment(pos);
		if (equipment == null)
		{
			return FindDecal(pos);
		}
		if (equipment.proto.isDecalHost)
		{
			Equipment equipment2 = FindDecal(pos);
			if (equipment2 != null)
			{
				return equipment2;
			}
		}
		return equipment;
	}

	private Equipment FindDecal(Vector2Int pos)
	{
		Equipment equipment = null;
		for (int i = 0; i < 3; i++)
		{
			Vector2Int cellpos = new Vector2Int(pos.x, pos.y + i);
			equipment = ((IEquipmentHost)currentRoom)?.GetDecalEquipment(cellpos);
			if (equipment != null)
			{
				break;
			}
		}
		if (equipment == null || equipment.TouchableAsDecal)
		{
			return equipment;
		}
		return null;
	}
}
