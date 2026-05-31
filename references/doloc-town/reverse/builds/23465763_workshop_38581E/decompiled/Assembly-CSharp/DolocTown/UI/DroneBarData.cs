using System.Linq;
using UnityEngine;

namespace DolocTown.UI;

public struct DroneBarData : IUIData
{
	public bool notEmpty { get; }

	public Sprite structure { get; }

	public int comCount { get; }

	public Sprite[] comTypes { get; }

	public Sprite[] comSprites { get; }

	public DroneBarData(ItemDroneStructure itemDrone)
	{
		this = default(DroneBarData);
		notEmpty = false;
		if (itemDrone != null)
		{
			notEmpty = true;
			structure = itemDrone.uiSprite;
			comCount = itemDrone.droneStructure.slots.Length;
			comTypes = itemDrone.droneStructure.slots.Select((DroneSlot com) => com.proto.GetSlotInfo().DefaultIcon.Asset).ToArray();
			comSprites = itemDrone.droneStructure.slots.Select((DroneSlot com) => com.Icon).ToArray();
		}
	}
}
